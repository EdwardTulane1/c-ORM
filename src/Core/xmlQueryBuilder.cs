using System.Reflection;
using MyORM.Helper;
using MyORM.Attributes;
using MyORM.Core;
using System.Xml.Linq;

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

    public XmlQueryBuilder(string basePath, string tableName)
    {
        _basePath = basePath;
        _tableName = tableName;
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
        if (!File.Exists(xmlPath))
        {
            Console.WriteLine($"zzzz file not found");
            return new List<T>();
        }

        var doc = XDocument.Load(xmlPath);
        var results = new List<T>();

        // Apply where conditions
        foreach (var element in doc.Root.Elements("Entity"))
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
                results.Add(entityElement);
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
        }

        return entities;
    }

    private List<T> ApplyOrdering(List<T> elements)
    {
        var ordered = new List<T>(elements);
        ordered.Sort((a, b) =>
        {
            foreach (var prop in _orderByProperties)
            {
                var valueA = a.GetType().GetProperty(prop)?.GetValue(a);
                var valueB = b.GetType().GetProperty(prop)?.GetValue(b);
                int comparison = string.Compare(valueA.ToString(), valueB.ToString());
                
                if (comparison != 0)
                {
                    return _orderDescending ? -comparison : comparison;
                }
            }
            return 0;
        });
        return ordered;
    }

    private List<T> ApplyPagination(List<T> elements)
    {
        var result = new List<T>();
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

        var elementType = elementValue.GetType();

        // Handle numeric types
        if (IsNumericType(elementType))
        {
            // Convert both values to decimal for consistent numeric comparison
            decimal numericElement = Convert.ToDecimal(elementValue);
            decimal numericCondition = Convert.ToDecimal(condition.Value);

            switch (condition.Operator.ToUpper())
            {
                case "=":
                    return numericElement == numericCondition;
                case ">":
                    return numericElement > numericCondition;
                case "<":
                    return numericElement < numericCondition;
                case ">=":
                    return numericElement >= numericCondition;
                case "<=":
                    return numericElement <= numericCondition;
                case "!=":
                    return numericElement != numericCondition;
                case "LIKE":
                    return elementValue.ToString().Contains(condition.Value.ToString(), StringComparison.OrdinalIgnoreCase);
                default:
                    throw new NotSupportedException($"Operator {condition.Operator} not supported");
            }
        }
        else
        {
            // String comparison
            string strElement = elementValue.ToString();
            string strCondition = condition.Value.ToString();

            switch (condition.Operator.ToUpper())
            {
                case "=":
                    return string.Equals(strElement, strCondition, StringComparison.OrdinalIgnoreCase);
                case ">":
                    return string.Compare(strElement, strCondition, StringComparison.OrdinalIgnoreCase) > 0;
                case "<":
                    return string.Compare(strElement, strCondition, StringComparison.OrdinalIgnoreCase) < 0;
                case ">=":
                    return string.Compare(strElement, strCondition, StringComparison.OrdinalIgnoreCase) >= 0;
                case "<=":
                    return string.Compare(strElement, strCondition, StringComparison.OrdinalIgnoreCase) <= 0;
                case "!=":
                    return !string.Equals(strElement, strCondition, StringComparison.OrdinalIgnoreCase);
                case "LIKE":
                    return strElement.Contains(strCondition, StringComparison.OrdinalIgnoreCase);
                default:
                    throw new NotSupportedException($"Operator {condition.Operator} not supported");
            }
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

    private T XmlToEntity(XElement element)
    {
        var entity = Activator.CreateInstance<T>();
        var properties = typeof(T).GetProperties();
        foreach (var prop in properties)
        {
            if (prop.GetCustomAttribute<ColumnAttribute>() != null || 
                prop.GetCustomAttribute<KeyAttribute>() != null)
            {
                var value = element.Element(prop.Name)?.Value;
                if (value != null)
                {
                    var convertedValue = Convert.ChangeType(value, prop.PropertyType);
                    prop.SetValue(entity, convertedValue);
                }
            }
        }
        return entity;
    }

    private T LoadRelatedEntities(T entity)
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
                        LoadManyToManyRelations(entity, prop, relAttr);
                        break;
                    case RelationType.ManyToOne:
                    case RelationType.OneToOne:
                        LoadSingleRelation(entity, prop, relAttr);
                        break;
                    case RelationType.OneToMany:
                        LoadOneToManyRelations(entity, prop, relAttr);
                        break;
                }
            }
        }
        return entity;
    }

    private void LoadManyToManyRelations(T entity, PropertyInfo prop, RelationshipAttribute relAttr)
    {
        var mappingFileName = HelperFuncs.getFileNameAlphaBetic(typeof(T).Name, relAttr.RelatedType.Name);
        var filePath = Path.Combine(_basePath, mappingFileName);
        
        if (!File.Exists(filePath)) return;

        var doc = XDocument.Load(filePath);
        var entityKeyValue = HelperFuncs.GetKeyProperty(typeof(T))
            .GetValue(entity)?.ToString();

        var relatedKeys = new List<string>();
        foreach (var rel in doc.Root.Elements("Relationship"))
        {
            if (rel.Element("ParentKey")?.Value == entityKeyValue)
            {
                var relatedKey = rel.Element("RelatedKey")?.Value;
                if (relatedKey != null)
                {
                    relatedKeys.Add(relatedKey);
                }
            }
        }

        var relatedEntities = LoadEntitiesByKeys(relAttr.RelatedType, relatedKeys);
        prop.SetValue(entity, relatedEntities);
    }

    private void LoadSingleRelation(T entity, PropertyInfo prop, RelationshipAttribute relAttr)
    {
        var foreignKeyProp = $"{relAttr.RelatedType.Name}_{HelperFuncs.GetKeyProperty(relAttr.RelatedType).Name}";
        var foreignKeyValue = entity.GetType()
            .GetProperty(foreignKeyProp)
            ?.GetValue(entity)
            ?.ToString();

        if (foreignKeyValue != null)
        {
            var relatedEntity = LoadEntityByKey(relAttr.RelatedType, foreignKeyValue);
            prop.SetValue(entity, relatedEntity);
        }
    }

    private void LoadOneToManyRelations(T entity, PropertyInfo prop, RelationshipAttribute relAttr)
    {
        var entityKeyValue = HelperFuncs.GetKeyProperty(typeof(T))
            .GetValue(entity)?.ToString();

        var relatedEntities = LoadRelatedEntities(relAttr.RelatedType, typeof(T), entityKeyValue);
        prop.SetValue(entity, relatedEntities);
    }

    private ICollection<object> LoadEntitiesByKeys(Type entityType, IEnumerable<string> keys)
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
                var entity = Activator.CreateInstance(entityType);
                // Set properties from XML
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

    private object LoadEntityByKey(Type entityType, string key)
    {
        var xmlPath = HelperFuncs.GetTablePath(_basePath, entityType.Name);
        if (!File.Exists(xmlPath)) return null;

        var doc = XDocument.Load(xmlPath);
        var keyProp = HelperFuncs.GetKeyProperty(entityType);

        foreach (var element in doc.Root.Elements("Entity"))
        {
            if (element.Element(keyProp.Name)?.Value == key)
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
                return entity;
            }
        }
        return null;
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