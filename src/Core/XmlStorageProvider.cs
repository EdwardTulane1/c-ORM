
using System.Xml.Linq;
using System.Reflection;
using MyORM.Attributes;
using MyORM.Attributes.Validation;

namespace MyORM.Core
{
    public class XmlStorageProvider
    {
        private readonly string _basePath;
        private readonly Dictionary<string, XDocument> _documents;

        public XmlStorageProvider(string basePath)
        {
            _basePath = basePath;
            _documents = new Dictionary<string, XDocument>();
            Directory.CreateDirectory(basePath);
        }

        public void SaveEntity<T>(T entity, string tableName) where T : Entity
        {
            var xmlPath = Path.Combine(_basePath, $"{tableName}.xml");
            Console.WriteLine($"Saving entity to: {xmlPath}");
            
            var doc = GetOrCreateDocument(tableName, xmlPath);
            var root = doc.Root;

            var entityElement = new XElement("Entity");
            
            // Debug all properties - Get properties from the actual type, not just Entity
            var actualType = entity.GetType();  // This gets the real type (Customer or Order)
            ValidationHelper.ValidateEntity(entity);
            Console.WriteLine($"\nDebug info for {actualType.Name}:");
            
            foreach (var prop in actualType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
                var keyAttr = prop.GetCustomAttribute<KeyAttribute>();
                var value = prop.GetValue(entity);
                
                Console.WriteLine($"Property: {prop.Name}");
                Console.WriteLine($"  Value: {value}");
                Console.WriteLine($"  Column Attribute: {columnAttr?.ColumnName ?? "none"}");
                Console.WriteLine($"  Key Attribute: {(keyAttr != null ? "yes" : "no")}");
                
                // The xml file contains only the properties with Column or Key attributes
                if (columnAttr != null || keyAttr != null)
                {
                    entityElement.Add(new XElement(prop.Name, value?.ToString() ?? ""));
                }
            }

            // Handle relationships
            HandleRelationshipProperties(entity, entityElement);

            // Find existing entity or add new one
            var keyProp = actualType.GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);

            if (keyProp != null && entity.IsModified)
            {
                var keyValue = keyProp.GetValue(entity)?.ToString();
                var existingEntity = root.Elements("Entity")
                    .FirstOrDefault(e => e.Element(keyProp.Name)?.Value == keyValue);

                if (existingEntity != null)
                {
                    existingEntity.ReplaceWith(entityElement);
                }
                else
                {
                    root.Add(entityElement);
                }
            }
            else
            {
                root.Add(entityElement);
            }

            doc.Save(xmlPath);
            Console.WriteLine($"Saved to {xmlPath}");
        }

        public void DeleteEntity<T>(T entity, string tableName) where T : Entity
        {
            var xmlPath = Path.Combine(_basePath, $"{tableName}.xml");
            var doc = GetOrCreateDocument(tableName, xmlPath);
            var root = doc.Root;

            var keyProp = typeof(T).GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);
            
            if (keyProp != null)
            {
                var keyValue = keyProp.GetValue(entity)?.ToString();
                var elementToRemove = root.Elements("Entity")
                    .FirstOrDefault(e => e.Element(keyProp.Name)?.Value == keyValue);
                
                elementToRemove?.Remove();
                doc.Save(xmlPath);
            }
        }

        private XDocument GetOrCreateDocument(string tableName, string xmlPath)
        {
            if (_documents.ContainsKey(tableName))
            {
                return _documents[tableName];
            }

            XDocument doc;
            if (File.Exists(xmlPath))
            {
                doc = XDocument.Load(xmlPath);
            }
            else
            {
                doc = new XDocument(new XElement("Entities"));
            }

            _documents[tableName] = doc;
            return doc;
        }







// RELATIONSHIP HANDLING. Entity is the class. EntityElement is the XML element building.
        private void HandleRelationshipProperties<T>(T entity, XElement entityElement) where T : Entity
        {
            var entityType = entity.GetType();
            var relationshipProps = entityType.GetProperties()
                .Where(p => p.GetCustomAttribute<RelationshipAttribute>() != null);

            // all the elements that have a relationship attribute
            foreach (var prop in relationshipProps)
            {
                // get the relationship attribute of each of them
                var relAttr = prop.GetCustomAttribute<RelationshipAttribute>();
                Console.WriteLine($"Handling relationship: {prop.Name} {relAttr.Type}, From: {relAttr.FromProperty} To: {relAttr.ToProperty}");
                
                switch (relAttr?.Type)
                {
                    case RelationType.ManyToMany:
                        // get the related objects  (the collection dbSet)
                        var collection = prop.GetValue(entity) as IEnumerable<Entity>;
                        if (collection != null && collection.Any())
                        {
                            var mappingFileName = $"{entityType.Name}_{relAttr.RelatedType.Name}.xml";
                            Console.WriteLine($"Saving many-to-many relationship to: {mappingFileName}");
                            // log all collection items
                            SaveRelationshipMapping(entity, collection, mappingFileName);
                        }
                        break;
                    case RelationType.ManyToOne:
                    case RelationType.OneToOne:
                        // Store the foreign key value
                        var foreignKeyProp = entityType.GetProperties()
                            .FirstOrDefault(p => p.Name == relAttr.FromProperty);
                        if (foreignKeyProp != null)
                        {
                            var foreignKeyValue = foreignKeyProp.GetValue(entity);
                            entityElement.Add(new XElement($"{foreignKeyProp.Name}_key", foreignKeyValue?.ToString() ?? ""));
                        }
                        break;

                    case RelationType.OneToMany:
                        // In this case, the related entities are stored in the related entity
                        break;
                }
            }
        }

        private void SaveRelationshipMapping<T>(T parentEntity, IEnumerable<Entity> relatedEntities, string relationshipFile) where T : Entity
        {
            try
            {
                // The mapping is always to the key in the related entity
                var parentType = parentEntity.GetType();
                Console.WriteLine($"Saving many-to-many relationships for {parentType.Name}");
                
                var filePath = Path.Combine(_basePath, relationshipFile);
                Console.WriteLine($"Relationship file path: {filePath}");

                XDocument doc;
                if (File.Exists(filePath))
                {
                    doc = XDocument.Load(filePath);
                    Console.WriteLine("Loaded existing relationship file");
                }
                else
                {
                    doc = new XDocument(new XElement("Relationships"));
                    Console.WriteLine("Created new relationship file");
                }

                var rootElement = doc.Root;
                
                // Get parent entity key using actual type
                var parentKeyProp = parentType.GetProperties()
                    .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);
                
                if (parentKeyProp == null)
                {
                    throw new InvalidOperationException($"No key property found for {parentType.Name}");
                }

                var parentKeyValue = parentKeyProp.GetValue(parentEntity)?.ToString();
                Console.WriteLine($"Parent key value: {parentKeyValue}");

                // Remove existing relationships for this parent
                var existingRelationships = rootElement.Elements("Relationship")
                    .Where(e => e.Element("ParentKey")?.Value == parentKeyValue)
                    .ToList();
                
                foreach (var rel in existingRelationships)
                {
                    rel.Remove();
                }

                // Add new relationships
                foreach (var relatedEntity in relatedEntities)
                {
                    var relatedType = relatedEntity.GetType();
                    var relatedKeyProp = relatedType.GetProperties()
                        .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);
                    
                    if (relatedKeyProp == null)
                    {
                        Console.WriteLine($"No key property found for related entity {relatedType.Name}");
                        continue;
                    }

                    var relatedKeyValue = relatedKeyProp.GetValue(relatedEntity)?.ToString();
                    Console.WriteLine($"Adding relationship: Parent {parentKeyValue} -> Related {relatedKeyValue}");

                    var relationshipElement = new XElement("Relationship",
                        new XElement("ParentKey", parentKeyValue),
                        new XElement("RelatedKey", relatedKeyValue),
                        new XElement("ParentType", parentType.Name),
                        new XElement("RelatedType", relatedType.Name)
                    );
                    
                    rootElement.Add(relationshipElement);
                }

                doc.Save(filePath);
                Console.WriteLine($"Saved relationships to {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving many-to-many relationships: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
