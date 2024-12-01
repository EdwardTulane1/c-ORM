/*
 * XmlStorageProvider handles the persistence of entities to XML files.
 * It manages the storage and retrieval of entities, including their relationships.
 * Each entity type (table) is stored in a separate XML file, with additional files for relationships.
 * This provider implements the storage strategy for the ORM using XML as the underlying storage mechanism.
 */

using System.Xml.Linq;
using System.Reflection;
using MyORM.Attributes;
using MyORM.Attributes.Validation;
using MyORM.Helper;

namespace MyORM.Core
{
    public class XmlStorageProvider
    {
        private readonly string _basePath;
        private readonly Dictionary<string, XDocument> _documents;

        /*
         * Initializes the storage provider with a base path for XML files
         * Creates the directory if it doesn't exist
         * @param basePath: Directory path where XML files will be stored
         */
        public XmlStorageProvider(string basePath)
        {
            _basePath = basePath;
            _documents = new Dictionary<string, XDocument>();
            Directory.CreateDirectory(basePath);
        }



         
        /*
         * Saves an entity to its corresponding XML file
         * Handles both new entities and updates to existing ones
         * @param entity: The entity to save
         * @param tableName: The name of the table/entity type
         */
        public void SaveEntity<T>(T entity, string tableName) where T : Entity
        {
            // each table has its own XML file (e.g. Customer.xml, Order.xml)
            var xmlPath = Path.Combine(_basePath, $"{tableName}.xml");
            Console.WriteLine($"Saving entity to: {xmlPath}");

            var doc = GetOrCreateDocument(tableName, xmlPath);
            var root = doc.Root;

            // each entity is an element in the XML file (e.g. <Entity>, <Entity>, <Entity>)
            var entityElement = new XElement("Entity");

            // Debug all properties - Get properties from the actual type, not just Entity
            var actualType = entity.GetType();  // This gets the real type (Customer or Order)
            ValidationHelper.ValidateEntity(entity); // validate the entity properties are valid

            foreach (var prop in actualType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
                var keyAttr = prop.GetCustomAttribute<KeyAttribute>();
                var value = prop.GetValue(entity);

              
                // The xml file contains only the properties with Column or Key attributes
                if (columnAttr != null || keyAttr != null)
                {
                    entityElement.Add(new XElement(prop.Name, value?.ToString() ?? ""));
                }
            }

            // Handle relationships
            HandleRelationshipProperties(entity, entityElement);

            if(entity.IsDeleted){// case the related entity was deleted and cascade was set
                return;
            }

            // Find existing entity or add new one
            var keyProp = actualType.GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);

            Console.WriteLine($"Type: {actualType.Name}, Key property: {keyProp?.Name}, IsModified: {entity.IsModified}, IsNew: {entity.IsNew}, value: {keyProp.GetValue(entity)?.ToString()}");
            if (keyProp != null && entity.IsModified) // the modified has to be anough but we double check the key
            {
                var keyValue = keyProp.GetValue(entity)?.ToString();
                var existingEntity = root.Elements("Entity")
                    .FirstOrDefault(e => e.Element(keyProp.Name)?.Value == keyValue);

                if (existingEntity != null)
                {
                    Console.WriteLine($"Replacing existing entity with key {keyValue}");
                    existingEntity.ReplaceWith(entityElement);
                }
                else
                {
                    Console.WriteLine($"Adding new entity with key {keyValue}");
                    root.Add(entityElement);
                }
            }
            else // neq entity
            {
                root.Add(entityElement);
            }

            doc.Save(xmlPath);
            Console.WriteLine($"Saved to {xmlPath}");
        }

        /*
         * Deletes an entity and its relationships from storage
         * Handles cleanup of related entities based on relationship type
         * @param entity: The entity to delete
         * @param tableName: The name of the table/entity type
         */
        public void DeleteEntity<T>(T entity, string tableName) where T : Entity
        {
            var xmlPath = Path.Combine(_basePath, $"{tableName}.xml");
            var doc = GetOrCreateDocument(tableName, xmlPath);
            var root = doc.Root;

            var entityType = entity.GetType();
            var keyProp = entityType.GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);

            if (keyProp != null)
            {
                var keyValue = keyProp.GetValue(entity)?.ToString();

                // Handle relationships before deleting the entity
                Console.WriteLine($"Deleting entity: {entity.GetType().Name} with key {keyValue}");
                HandleEntityDeletion(entity, keyValue);

                // Delete the entity itself
                var elementToRemove = root.Elements("Entity")
                    .FirstOrDefault(e => e.Element(keyProp.Name)?.Value == keyValue);

                elementToRemove?.Remove();
                doc.Save(xmlPath);
            }
        }

        /*
         * Gets or creates an XML document for a table
         * Caches documents to avoid repeated file I/O
         * @param tableName: The name of the table
         * @param xmlPath: The file path for the XML document
         * @returns: The XDocument for the specified table
         */
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

        /*
         * Handles the storage of entity relationships
         * Processes different relationship types (OneToMany, ManyToMany, etc.)
         * @param entity: The entity whose relationships are being handled
         * @param entityElement: The XML element representing the entity
         */
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
                Console.WriteLine($"Handling relationship: {prop.Name} {relAttr.Type}, From: {relAttr.RelatedType} To: {relAttr.Type}");

                switch (relAttr?.Type)
                {
                    case RelationType.ManyToMany:
                        // get the related objects (the collection dbSet) and filter out deleted entities
                        var collection = (prop.GetValue(entity) as IEnumerable<Entity>)?
                            .Where(e => !e.IsDeleted)
                            .ToList();

                        if (collection != null && collection.Any())
                        {
                            var mappingFileName = HelperFuncs.getFileNameAlphaBetic(entityType.Name, relAttr.RelatedType.Name);
                            Console.WriteLine($"Saving many-to-many relationship to: {mappingFileName}");
                            SaveRelationshipMapping(entity, collection, mappingFileName);
                        }
                        break;
                    case RelationType.ManyToOne:
                    case RelationType.OneToOne: // realtions In depend on
                        // Store the foreign key value
                        // get key attribute of the related entity
                        var foreignKeyProp = HelperFuncs.GetKeyProperty(relAttr.RelatedType);
                        Console.WriteLine($"foreign entity type: {relAttr.RelatedType.Name}");
                        Console.WriteLine($"Foreign key property: {foreignKeyProp.Name}");
                        Console.WriteLine($"Foreign key value: {foreignKeyProp.GetValue(prop.GetValue(entity))?.ToString()}");
                            
                        if (foreignKeyProp != null)
                        {
                            // gets the keyattribute value of the related entity3
                            var foreignKeyValue = foreignKeyProp.GetValue(prop.GetValue(entity))?.ToString()!;
                            // check if the related entity wasn't deleted


                            var isDeleted = HelperFuncs.IsEntityDeleted(relAttr.RelatedType, foreignKeyValue);
                            if (isDeleted){
                                Console.WriteLine($"Related entity {relAttr.RelatedType.Name} with key {foreignKeyValue} was deleted");
                                // check for onDelete behavior
                                switch(relAttr.OnDelete){
                                    case DeleteBehavior.SetNull:
                                        foreignKeyProp.SetValue(entity, null);
                                        break;
                                    case DeleteBehavior.Cascade:
                                        // delete the entity
                                        entity.IsDeleted = true;
                                        DeleteEntity(entity, entityType.Name);
                                        break;
                                    case DeleteBehavior.Restrict:
                                        // do nothing
                                        break;
                                }
                            }
                            // type and foreign key property name
                            entityElement.Add(new XElement($"{relAttr.RelatedType.Name}_{foreignKeyProp.Name}", foreignKeyValue?.ToString() ?? ""));
                            
                        }
                        break;

                    case RelationType.OneToMany:
                        // In this case, the related entities are stored in the related entity
                        break;
                }
            }
        }

        /*
         * Saves many-to-many relationship mappings to a separate XML file
         * Creates or updates relationship records between entities
         * @param parentEntity: The main entity in the relationship
         * @param relatedEntities: Collection of related entities
         * @param relationshipFile: Name of the file to store relationships
         */
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
                var parentKeyProp = HelperFuncs.GetKeyProperty(parentType);

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
                    var relatedKeyProp =  HelperFuncs.GetKeyProperty(relatedType);
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

        /*
         * Handles the deletion of entity relationships
         * Cleans up all relationship records when an entity is deleted
         * @param entity: The entity being deleted
         * @param entityKeyValue: The key value of the entity
         */
        private void HandleEntityDeletion<T>(T entity, string entityKeyValue) where T : Entity
        {
            var entityType = entity.GetType();
            var relationshipProps = entityType.GetProperties()
                .Where(p => p.GetCustomAttribute<RelationshipAttribute>() != null);

            foreach (var prop in relationshipProps)
            {
                var relAttr = prop.GetCustomAttribute<RelationshipAttribute>();
                Console.WriteLine($"Handling deletion of relationship: {prop.Name} {relAttr.Type}");

                switch (relAttr?.Type)
                {
                    case RelationType.ManyToMany:
                        // Delete all relationship mappings for this entity
                        DeleteManyToManyRelationships(entityType, relAttr.RelatedType, entityKeyValue);
                        break;

                    case RelationType.OneToMany:
                        // Find and update or delete related entities
                        HandleOneToManyDeletion(entity, relAttr, entityKeyValue);
                        break;

                    case RelationType.ManyToOne:
                    case RelationType.OneToOne: // TODO
                        //HandleOneToOneDeletion(entity, relAttr, entityKeyValue);
                        break;
                }
            }
        }

        /*
         * Deletes many-to-many relationship records
         * Removes all relationship mappings for a given entity
         * @param parentType: Type of the parent entity
         * @param relatedType: Type of the related entity
         * @param parentKeyValue: Key value of the parent entity
         */
        private void DeleteManyToManyRelationships(Type parentType, Type relatedType, string parentKeyValue)
        {
            var mappingFileName = HelperFuncs.getFileNameAlphaBetic(parentType.Name, relatedType.Name);
            Console.WriteLine($"Deleting many-to-many relationships for {parentType.Name} and {relatedType.Name}");
            var filePath = Path.Combine(_basePath, mappingFileName);

            if (!File.Exists(filePath)) return;

            var doc = XDocument.Load(filePath);
            var rootElement = doc.Root;

            // Remove all relationships where this entity is the parent
            var relationshipsToRemove = rootElement.Elements("Relationship")
                .Where(e => e.Element("ParentKey")?.Value == parentKeyValue)
                .ToList();

            foreach (var rel in relationshipsToRemove)
            {
                rel.Remove();
            }

            // Also remove relationships where this entity is the related entity
            relationshipsToRemove = rootElement.Elements("Relationship")
                .Where(e => e.Element("RelatedKey")?.Value == parentKeyValue)
                .ToList();

            foreach (var rel in relationshipsToRemove)
            {
                rel.Remove();
            }

            doc.Save(filePath);
        }

        /*
         * Handles deletion of one-to-many relationships
         * Either deletes or updates related entities based on relationship requirements
         * @param entity: The entity being deleted
         * @param relAttr: The relationship attribute
         * @param parentKeyValue: Key value of the parent entity
         */
        private void HandleOneToManyDeletion<T>(T entity, RelationshipAttribute relAttr, string parentKeyValue) where T : Entity
        {
            // Get the XML file for the related entities
            var relatedTableName = relAttr.RelatedType.Name;
            var relatedXmlPath = Path.Combine(_basePath, $"{relatedTableName}.xml");

            if (!File.Exists(relatedXmlPath)) return;

            var doc = XDocument.Load(relatedXmlPath);
            var root = doc.Root;

            // Find all related entities that reference this entity
            var relatedEntities = root.Elements("Entity")
                .Where(e => e.Element($"{relAttr.RelatedType.Name}_{HelperFuncs.GetKeyProperty(relAttr.RelatedType).Name}")?.Value == parentKeyValue)
                .ToList();

            // Remove or update the related entities based on your business logic
            foreach (var relatedEntity in relatedEntities)
            {
                // check if foreign key property is required


                // check if the foreign key property is required
                var foreignKeyProp = relatedEntity.GetType().GetProperties()
                    .FirstOrDefault(p => p.Name == $"{relAttr.RelatedType.Name}_{HelperFuncs.GetKeyProperty(relAttr.RelatedType).Name}");

                var foreignKeyElement = relatedEntity.Element($"{relAttr.RelatedType.Name}_{HelperFuncs.GetKeyProperty(relAttr.RelatedType).Name}");
                // check if the foreign key property is required
                if (foreignKeyProp != null && foreignKeyElement != null) 
                {
                    // check onDelete behavior
                    switch(relAttr.OnDelete){
                        case DeleteBehavior.SetNull:
                            foreignKeyElement.Value = null;
                            break;
                        case DeleteBehavior.Cascade:
                            // delete the entity
                            relatedEntity.Remove();
                            break;
                        case DeleteBehavior.Restrict:
                            // do nothing
                            break;
                    }
                }
            }
            doc.Save(relatedXmlPath);
        }
    }
}
