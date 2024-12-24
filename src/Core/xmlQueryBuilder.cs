using System.Reflection;
using MyORM.Helper;
using MyORM.Attributes;
using MyORM.Core;
using System.Xml.Linq;
using System.Xml;

namespace MyORM.Core;

public class XmlQueryBuilder<T> where T : Entity
{
    private List<QueryCondition> _conditions = new List<QueryCondition>();
    private List<string> _orderByProperties = new List<string>();
    private bool _orderDescending = false;
    private int? _take = null;
    private int? _skip = null;
    private readonly string _basePath;
    private readonly string _tableName;
    private readonly DbSet<T> _dbSet;
    public XmlQueryBuilder(string basePath, string tableName, DbSet<T> dbSet)
    {
        _basePath = basePath;
        _tableName = tableName;
        _dbSet = dbSet;
    }

    public record EntityPlusXml<TEntity>(TEntity Entity, XElement xmlElement);
    public class EntityPlusXmlList<Z> : List<EntityPlusXml<Z>> {
        public EntityPlusXmlList(List<EntityPlusXml<Z>> list) : base(list) { }
        public EntityPlusXmlList() : base() { }
    }



    public XmlQueryBuilder<T> Where(string propertyName, string op, object value)
    {
        _conditions.Add(new QueryCondition(propertyName, op, value));
        return this;
    }

    public XmlQueryBuilder<T> OrderBy(string propertyName, bool descending = false)
    {
        _orderByProperties.Add(propertyName);
        _orderDescending = descending;
        return this;
    }

    public XmlQueryBuilder<T> Take(int count)
    {
        _take = count;
        return this;
    }

    public XmlQueryBuilder<T> Skip(int count)
    {
        _skip = count;
        return this;
    }

    public List<T> Execute()
    {
        var xmlPath = HelperFuncs.GetTablePath(_basePath, _tableName);
        // Console.WriteLine($"Executing query on table: {xmlPath}");

        if (!File.Exists(xmlPath))
        {
            Console.WriteLine($"zzzz file not found");
            return new List<T>();
        }

        var doc = XDocument.Load(xmlPath)!;
        var results = new EntityPlusXmlList<T>();  // Changed to tuple list

        // Apply where conditions
        foreach (var element in doc!.Root!.Elements("Entity"))
        {
            var entityElement = HelperFuncs.XmlToEntity<T>(element);
            bool matchesAllConditions = true;
            foreach (var condition in _conditions)
            {
                if (!EvaluateCondition(entityElement, condition))
                {
                    matchesAllConditions = false;
                    break;
                }
            }
            if (matchesAllConditions)
            {
                results.Add(new EntityPlusXml<T>(entityElement, element));
            }
        }

        // Apply ordering
        if (_orderByProperties.Count > 0)
        {
            results = ApplyOrdering(results);
        }

        // Apply pagination
        var finalResults = ApplyPagination(results);

        // Convert XElements to entities and load their relationships
        var entities = new List<T>();
        foreach (var element in finalResults)
        {
            entities.Add(LoadRelatedEntities(element));


            // IN HERE  BIG TODO: I HAVE TO ATTACH TRACKER TO THE ENTITY AND THE RELATED ENTITIES
            _dbSet.TrackEntity(element.Entity);
        }

        return entities;
    }

    private EntityPlusXmlList<T> ApplyOrdering(EntityPlusXmlList<T> elements)
    {
        var ordered = new EntityPlusXmlList<T>(elements);
        ordered.Sort((a, b) =>
        {
            foreach (var prop in _orderByProperties)
            {
                var valueA = a.Entity.GetType().GetProperty(prop)?.GetValue(a.Entity);
                var valueB = b.Entity.GetType().GetProperty(prop)?.GetValue(b.Entity);
                int comparison = CompareValues(valueA, valueB);
                
                if (comparison != 0)
                {
                    return _orderDescending ? -comparison : comparison;
                }
            }
            return 0;
        });
        return ordered;
    }

    private EntityPlusXmlList<T> ApplyPagination(EntityPlusXmlList<T> elements)
    {
        var result = new EntityPlusXmlList<T>();
        int startIndex = _skip.GetValueOrDefault(0);
        int endIndex = _take.HasValue ? 
            Math.Min(startIndex + _take.Value, elements.Count) : 
            elements.Count;

        for (int i = startIndex; i < endIndex; i++)
        {
            result.Add(elements[i]);
        }
        return result;
    }

    private bool EvaluateCondition(T entityElement, QueryCondition condition)
    {
        var elementValue = entityElement.GetType().GetProperty(condition.PropertyName)?.GetValue(entityElement);
        if (elementValue == null)
        {
            return false;
        }

        switch (condition.Operator.ToUpper())
        {
            case "=":
                return CompareValues(elementValue, condition.Value) == 0;
            case ">":
                return CompareValues(elementValue, condition.Value) > 0;
            case "<":
                return CompareValues(elementValue, condition.Value) < 0;
            case ">=":
                return CompareValues(elementValue, condition.Value) >= 0;
            case "<=":
                return CompareValues(elementValue, condition.Value) <= 0;
            case "!=":
                return CompareValues(elementValue, condition.Value) != 0;
            case "LIKE":
                return elementValue.ToString()!.Contains(condition.Value.ToString()!, StringComparison.OrdinalIgnoreCase);
            default:
                throw new NotSupportedException($"Operator {condition.Operator} not supported");
        }
    }

    private int CompareValues(object? valueA, object? valueB)
    {
        if (valueA == null || valueB == null)
        {
            return valueA == null && valueB == null ? 0 : (valueA == null ? -1 : 1);
        }

        var type = valueA.GetType();
        
        if (IsNumericType(type))
        {
            decimal numA = Convert.ToDecimal(valueA);
            decimal numB = Convert.ToDecimal(valueB);
            return numA.CompareTo(numB);
        }
        else
        {
            return string.Compare(valueA.ToString(), valueB.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }

    private bool IsNumericType(Type type)
    {
        if (type.IsEnum) return false;

        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Single:
                return true;
            default:
                return false;
        }
    }

    private T LoadRelatedEntities(EntityPlusXml<T> entityElement)
    {
        var properties = typeof(T).GetProperties();
        foreach (var prop in properties)
        {
            var relAttr = prop.GetCustomAttribute<RelationshipAttribute>();
            if (relAttr != null)
            {
                switch (relAttr.Type)
                {
                    case RelationType.ManyToMany:
                        LoadManyToManyRelations(entityElement, prop, relAttr);
                        break;
                    case RelationType.ManyToOne:
                    case RelationType.OneToOne:
                        LoadSingleRelation(entityElement, prop, relAttr);
                        break;
                    case RelationType.OneToMany:
                        LoadOneToManyRelations(entityElement, prop, relAttr);
                        break;
                }
            }
        }
        return entityElement.Entity;
    }

    private void LoadManyToManyRelations(EntityPlusXml<T> entityElement, PropertyInfo prop, RelationshipAttribute relAttr)
    {
        var mappingFileName = HelperFuncs.getFileNameAlphaBetic(typeof(T).Name, relAttr.RelatedType.Name);
        var filePath = Path.Combine(_basePath, mappingFileName);
        
        if (!File.Exists(filePath)) return;

        var doc = XDocument.Load(filePath);
        var entityKeyValue = HelperFuncs.GetKeyProperty(typeof(T))
            .GetValue(entityElement.Entity)?.ToString();

        // Create the correct list type
        var listType = typeof(List<>).MakeGenericType(relAttr.RelatedType);
        var relatedEntities = (System.Collections.IList)Activator.CreateInstance(listType, 0);

        // Find relationships where this entity appears under its type name
        foreach (var rel in doc.Root.Elements("Relationship"))
        {
            if (rel.Element(typeof(T).Name)?.Value == entityKeyValue)
            {
                var relatedKey = rel.Element(relAttr.RelatedType.Name)?.Value;
                if (relatedKey != null)
                {
                    var relatedEntity = HelperFuncs.LoadEntityByKey(relAttr.RelatedType, relatedKey);
                    if (relatedEntity != null)
                    {
                        relatedEntities.Add(relatedEntity);
                    }
                }
            }
        }

        prop.SetValue(entityElement.Entity, relatedEntities);
    }

    private void LoadSingleRelation(EntityPlusXml<T> entityElement, PropertyInfo prop, RelationshipAttribute relAttr)
    {
        var foreignKeyProp = $"{relAttr.RelatedType.Name}_{HelperFuncs.GetKeyProperty(relAttr.RelatedType).Name}";
        // get entity from the xml
        // how to log tyhe entity with all its properties and values

        var foreignKeyValue = entityElement.xmlElement.Element(foreignKeyProp)?.Value;

        //console.WriteLine($"foreignKeyProp: {foreignKeyProp}, foreignKeyValue: {foreignKeyValue}");
        if (foreignKeyValue != null)
        {
            //console.WriteLine($"foreignKeyValue: {foreignKeyValue}");
            var relatedEntity = HelperFuncs.LoadEntityByKey(relAttr.RelatedType, foreignKeyValue);
            //console.WriteLine($"relatedEntity: {relatedEntity}");
            prop.SetValue(entityElement.Entity, relatedEntity);
        }
    }

    private void LoadOneToManyRelations(EntityPlusXml<T> entityElement, PropertyInfo prop, RelationshipAttribute relAttr)
    {
        // Get the key value of the current entity
        var entityKeyProp = HelperFuncs.GetKeyProperty(typeof(T));
        var entityKeyValue = entityKeyProp.GetValue(entityElement.Entity)?.ToString();
            
        var foreignKeyProp = $"{entityElement.Entity.GetType().Name}_{entityKeyProp.Name}";

        var relatedEntityFilePath = HelperFuncs.GetTablePath(_basePath, relAttr.RelatedType.Name);
        if (!File.Exists(relatedEntityFilePath)) return;

        var doc = XDocument.Load(relatedEntityFilePath);


        // var relatedEntities = new List<object>();

         var listType = typeof(List<>).MakeGenericType(relAttr.RelatedType);
         var relatedEntities = (System.Collections.IList)Activator.CreateInstance(listType, 0);  // Use non-generic IList


        foreach (var element in doc?.Root?.Elements("Entity"))
        {
            var elementKey = element.Element(foreignKeyProp)?.Value;
            if (elementKey != null && elementKey == entityKeyValue)
            {
                // var zz = HelperFuncs.XmlToEntity(element, relAttr.RelatedType);
                // //console.WriteLine($"zz: {zz}");
                relatedEntities.Add(HelperFuncs.XmlToEntity(element, relAttr.RelatedType));
            }
            
        }

        //console.WriteLine($" Prop name: {prop.Name}, relatedEntities: {relatedEntities}");
        prop.SetValue(entityElement.Entity, relatedEntities);       
    }

    private List<object> LoadEntitiesByKeys(Type entityType, List<string> keys)
    {
        var entities = new List<object>();
        var xmlPath = HelperFuncs.GetTablePath(_basePath, entityType.Name);
        if (!File.Exists(xmlPath)) return entities;

        var doc = XDocument.Load(xmlPath);
        var keyProp = HelperFuncs.GetKeyProperty(entityType);

        foreach (var element in doc.Root.Elements("Entity"))
        {
            var elementKey = element.Element(keyProp.Name)?.Value;
            if (elementKey != null && keys.Contains(elementKey))
            {
                var entity = HelperFuncs.XmlToEntity(element, entityType);
                Console.WriteLine($"entity: {entity.GetType().Name},");
                entities.Add(entity);
            }
        }
        return entities;
    }

    

    private ICollection<object> LoadRelatedEntities(Type entityType, Type parentType, string parentKeyValue)
    {
        var entities = new List<object>();
        var xmlPath = HelperFuncs.GetTablePath(_basePath, entityType.Name);
        if (!File.Exists(xmlPath)) return entities;

        var doc = XDocument.Load(xmlPath);
        var foreignKeyName = $"{parentType.Name}_{HelperFuncs.GetKeyProperty(parentType).Name}";

        foreach (var element in doc.Root.Elements("Entity"))
        {
            if (element.Element(foreignKeyName)?.Value == parentKeyValue)
            {
                var entity = Activator.CreateInstance(entityType);
                foreach (var prop in entityType.GetProperties())
                {
                    if (prop.GetCustomAttribute<ColumnAttribute>() != null || 
                        prop.GetCustomAttribute<KeyAttribute>() != null)
                    {
                        var value = element.Element(prop.Name)?.Value;
                        if (value != null)
                        {
                            prop.SetValue(entity, Convert.ChangeType(value, prop.PropertyType));
                        }
                    }
                }
                entities.Add(entity);
            }
        }
        return entities;
    }

    
}

public class QueryCondition
{
    public string PropertyName { get; set; }
    public object Value { get; set; }
    public string Operator { get; set; }  // "=", ">", "<", "LIKE", etc.

    public QueryCondition(string propertyName, string op, object value)
    {
        PropertyName = propertyName;
        Operator = op;
        Value = value;
    }
}