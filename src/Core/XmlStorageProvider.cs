/*
 * XmlStorageProvider handles the persistence of entities to XML files.
 * It manages the storage and retrieval of entities, including their relationships.
 * Each entity type (table) is stored in a separate XML file, with additional files for relationships.
 * This provider implements the storage strategy for the ORM using XML as the underlying storage mechanism.
 */

using System.Xml.Linq;
using System.Reflection;
using MyORM.Attributes;
using MyORM.Helper;
using MyORM.Core;

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
            var xmlPath = HelperFuncs.GetTablePath(_basePath, tableName);
            var doc = GetOrCreateDocument(tableName, xmlPath);
            var root = doc.Root;

            // each entity is an element in the XML file (e.g. <Entity>, <Entity>, <Entity>)
            var entityElement = new XElement("Entity");
            //Get properties from the actual type, not just Entity
            var actualType = entity.GetType();  // This gets the real type (Customer or Order)
            // loops through all the properties of the entity
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

            if (entity.IsDeleted)
            {// case the related entity was deleted and cascade was set
                return;
            }

            // Find existing entity or add new one
            var keyProp = HelperFuncs.GetKeyProperty(actualType); // the key property of the entity (not the value of it)

            //console.WriteLine($"Type: {actualType.Name}, Key property: {keyProp?.Name}, IsModified: {entity.IsModified}, IsNew: {entity.IsNew}, value: {keyProp.GetValue(entity)?.ToString()}");


            if (keyProp != null && entity.IsModified) // the modified has to be anough but we double check the key. if a user created a new with the same key its a bug!!
            {
                var keyValue = keyProp.GetValue(entity)?.ToString();
                var existingEntity = root.Elements("Entity")
                    .FirstOrDefault(e => e.Element(keyProp.Name)?.Value == keyValue);

                if (existingEntity != null)
                {
                    //console.WriteLine($"Replacing existing entity with key {keyValue}");
                    existingEntity.ReplaceWith(entityElement);
                }
                else
                {
                    //console.WriteLine($"Adding new entity with key {keyValue}");
                    root.Add(entityElement);
                }
            }
            else // TODO -  check if etity exists in db!!?? -  no. If he assigned with same id with a NEW keyword its his fault
            {
                root.Add(entityElement);
            }

            doc.Save(xmlPath);
            //console.WriteLine($"Saved to {xmlPath}");
        }

        /*
         * Deletes an entity and its relationships from storage
         * Handles cleanup of related entities based on relationship type
         * @param entity: The entity to delete
         * @param tableName: The name of the table/entity type
         */
        public void DeleteEntity<T>(T entity, string tableName) where T : Entity
        {
            var xmlPath = HelperFuncs.GetTablePath(_basePath, tableName);
            var doc = GetOrCreateDocument(tableName, xmlPath);
            var root = doc.Root;

            var entityType = entity.GetType();
            var keyProp = entityType.GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);

            if (keyProp != null)
            {
                var keyValue = keyProp.GetValue(entity)?.ToString();

                // Handle relationships before deleting the entity
                //console.WriteLine($"Deleting entity: {entity.GetType().Name} with key {keyValue}");
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
                //console.WriteLine($"Handling relationship: {prop.Name} {relAttr.Type}, From: {relAttr.RelatedType} To: {relAttr.Type}");

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
                            //console.WriteLine($"Saving many-to-many relationship to: {mappingFileName}");
                            SaveRelationshipMapping(entity, collection, mappingFileName);
                        }
                        break;
                    case RelationType.ManyToOne:
                    case RelationType.OneToOne: // realtions In depend on
                        // Store the foreign key value
                        // get key attribute of the related entity
                        var foreignKeyProp = HelperFuncs.GetKeyProperty(relAttr.RelatedType);
                        //console.WriteLine($"foreign entity type: {relAttr.RelatedType.Name}");
                        //console.WriteLine($"Foreign key property: {foreignKeyProp.Name}");
                        //console.WriteLine($"Foreign key value: {foreignKeyProp.GetValue(prop.GetValue(entity))?.ToString()}");

                        if (foreignKeyProp != null)
                        {
                            // gets the keyattribute value of the related entity3
                            var relatedEntity = prop.GetValue(entity) as Entity;
                            if(relatedEntity == null)
                            {
                                continue;
                            }
                            var foreignKeyValue = foreignKeyProp.GetValue(relatedEntity)?.ToString()!;
                            // check if the related entity wasn't deleted


                            var isDeleted = HelperFuncs.IsEntityDeleted(relAttr.RelatedType, foreignKeyValue);
                            if (isDeleted)
                            {
                                //console.WriteLine($"Related entity {relAttr.RelatedType.Name} with key {foreignKeyValue} was deleted");
                                // check for onDelete behavior
                                switch (relAttr.OnDelete)
                                {
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
        private void SaveRelationshipMapping<T>(T entity, IEnumerable<Entity> relatedEntities, string relationshipFile) where T : Entity
        {
            try
            {
                var entityType = entity.GetType();
                var filePath = Path.Combine(_basePath, relationshipFile);

                XDocument doc = File.Exists(filePath) 
                    ? XDocument.Load(filePath)
                    : new XDocument(new XElement("Relationships"));

                var rootElement = doc.Root;
                var entityKeyProp = HelperFuncs.GetKeyProperty(entityType);

            

                var entityKeyValue = entityKeyProp.GetValue(entity)?.ToString();

                // Remove existing relationships for this entity
                var existingRelationships = rootElement.Elements("Relationship")
                    .Where(e => e.Element(entityType.Name)?.Value == entityKeyValue)
                    .ToList();

                foreach (var rel in existingRelationships)
                {
                    rel.Remove();
                }

                // Add new relationships
                foreach (var relatedEntity in relatedEntities)
                {
                    var relatedType = relatedEntity.GetType();
                    var relatedKeyProp = HelperFuncs.GetKeyProperty(relatedType);
                    if (relatedKeyProp == null) continue;

                    var relatedKeyValue = relatedKeyProp.GetValue(relatedEntity)?.ToString();

                    var relationshipElement = new XElement("Relationship",
                        new XElement(entityType.Name, entityKeyValue),
                        new XElement(relatedType.Name, relatedKeyValue)
                    );

                    rootElement.Add(relationshipElement);
                }

                doc.Save(filePath);
            }
            catch (Exception ex)
            {
                // Handle exception
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
                //console.WriteLine($"Handling deletion of relationship: {prop.Name} {relAttr.Type}");

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

                    case RelationType.ManyToOne: // If I'm many to one then the relatuionship is saved on my side. Once I'm delted the relationship is deleted and all good
                    case RelationType.OneToOne: // TODO BIG TODO - 
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
        private void DeleteManyToManyRelationships(Type entityType, Type relatedType, string entityKeyValue)
        {
            var mappingFileName = HelperFuncs.getFileNameAlphaBetic(entityType.Name, relatedType.Name);
            var filePath = Path.Combine(_basePath, mappingFileName);

            if (!File.Exists(filePath)) return;

            var doc = XDocument.Load(filePath);
            var rootElement = doc.Root;

            // Remove all relationships where this entity appears
            var relationshipsToRemove = rootElement.Elements("Relationship")
                .Where(e => e.Element(entityType.Name)?.Value == entityKeyValue ||
                           e.Element(relatedType.Name)?.Value == entityKeyValue)
                .ToList();

            foreach (var rel in relationshipsToRemove)
            {
                var relatedKey = rel.Element(relatedType.Name)?.Value;

                var relatedAType = Assembly.GetExecutingAssembly().GetTypes()
                    .FirstOrDefault(t => t.Name == relatedType.Name);

                if (relatedAType != null)
                {
                    var relatedEntity = (Entity)HelperFuncs.LoadEntityByKey(relatedAType, relatedKey);
                    if (relatedEntity != null)
                    {
                        relatedEntity.IsDeleted = true;
                        HelperFuncs.TrackDeletedEntity(relatedEntity);
                        DeleteEntity(relatedEntity, relatedAType.Name);
                    }
                }

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

            var relationProperty  = $"{entity.GetType().Name}_{HelperFuncs.GetKeyProperty(relAttr.RelatedType).Name}";

            // Find all related entities that reference this entity
            var relatedEntities = root.Elements("Entity")
                .Where(e => e.Element(relationProperty)?.Value == parentKeyValue)
                .ToList();

            // Remove or update the related entities based on your business logic
            foreach (var relatedEntityXML in relatedEntities)
            {
                // check if foreign key property is required
                // check if the foreign key property is required
                var relatedEntity = HelperFuncs.XmlToEntity(relatedEntityXML, relAttr.RelatedType);
                var foreignKeyProp = relatedEntityXML.Element(relationProperty);
                var foreignKeyElement = relatedEntityXML.Element(relationProperty);

                // check if the foreign key property is required
                if (foreignKeyProp != null && foreignKeyElement != null)
                {
                    // check onDelete behavior
                    //console.WriteLine($"onDelete behavior: {relAttr.OnDelete}, attirbute to delete: {foreignKeyElement.Value}");
                    switch (relAttr.OnDelete)
                    {
                        case DeleteBehavior.SetNull:
                            foreignKeyElement.Remove();
                            break;
                        case DeleteBehavior.Cascade:
                            // GET TYPE OF ENTITY AND DELETE IT PROPERLY - BIG TODO
                            //console.WriteLine($"deleting entity: {relatedEntity.GetType().Name}, ");
                            relatedEntityXML.Remove(); // but what with its relations???
                            break;
                        case DeleteBehavior.Restrict:
                            // do nothing
                            break;
                    }
                }
            }
            doc.Save(relatedXmlPath);
        }





        // public IQueryable<T> Query<T>() where T : Entity
        // {
        //     var builder = new XmlQueryBuilder<T>(_basePath, typeof(T).Name);
        //     return builder.Execute().AsQueryable();
        // }
    }
}
