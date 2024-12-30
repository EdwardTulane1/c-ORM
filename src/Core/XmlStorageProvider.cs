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
            var xmlDocument = GetOrCreateDocument(tableName, xmlPath);
            var rootElement = xmlDocument.Root;
            var newEntityElement = new XElement("Entity");
            var entityType = entity.GetType();

            // each entity is an element in the XML file (e.g. <Entity>, <Entity>, <Entity>)
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
                    newEntityElement.Add(new XElement(prop.Name, value?.ToString() ?? ""));
                }
            }

            // Handle relationships
            saveRelationships(entity, newEntityElement);

            // Find existing entity or add new one
            var keyProp = HelperFuncs.GetKeyProperty(actualType); // the key property of the entity (not the value of it)

            //console.WriteLine($"Type: {actualType.Name}, Key property: {keyProp?.Name}, IsModified: {entity.IsModified}, IsNew: {entity.IsNew}, value: {keyProp.GetValue(entity)?.ToString()}");

            if (keyProp != null && entity.IsModified) // the modified has to be anough but we double check the key. if a user created a new with the same key its a bug!!
            {
                var keyValue = keyProp.GetValue(entity)?.ToString();
                var existingEntity = HelperFuncs.GetEntityByKey(rootElement, keyProp, keyValue);

                if (existingEntity != null)
                {
                    //console.WriteLine($"Replacing existing entity with key {keyValue}");
                    existingEntity.ReplaceWith(newEntityElement);
                }
                else
                {
                    rootElement.Add(newEntityElement);
                }
            }
            else
            {
                rootElement.Add(newEntityElement);
            }

            xmlDocument.Save(xmlPath);
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
            var keyValue = keyProp?.GetValue(entity)?.ToString();


            if (keyValue != null)
            {
                // Handle relationships before deleting the entity
                Console.WriteLine($"Deleting entity: {entity.GetType().Name} with key {keyValue}");
                HandleEntityDeletion(entity, keyValue);

                // Delete the entity itself
                var elementToRemove = HelperFuncs.GetEntityByKey(root, keyProp, keyValue);
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
        private void saveRelationships<T>(T entity, XElement entityElement) where T : Entity
        {
            var currentEntityType = entity.GetType();
            var relationshipProperties = HelperFuncs.GetRelationshipProperties(currentEntityType);

            // all the elements that have a relationship attribute
            foreach (var prop in relationshipProperties)
            {
                // get the relationship attribute of each of them
                var relationshipAttribute = prop.GetCustomAttribute<RelationshipAttribute>();
                //console.WriteLine($"Handling relationship: {prop.Name} {relAttr.Type}, From: {relAttr.RelatedType} To: {relAttr.Type}");

                switch (relationshipAttribute?.Type)
                {
                    case RelationType.ManyToMany:
                        // get the related objects (the collection dbSet) and filter out deleted entities
                        var relatedEntityType = relationshipAttribute?.RelatedType;
                        var relatedEntities = (prop.GetValue(entity) as IEnumerable<Entity>)?
                            .Where(e => !e.IsDeleted)
                            .ToList();

                        if (relatedEntities != null && relatedEntities.Any())
                        {
                            var mappingFileName = HelperFuncs.getFileNameAlphaBetic(currentEntityType.Name, relatedEntityType?.Name);
                            SaveRelationshipMapping(entity, relatedEntities, mappingFileName);
                        }
                        break;
                        
                    case RelationType.OneToOne: // realtions In depend on
                        if(relationshipAttribute.OnDelete != DeleteBehavior.Orphan){
                            break; //means that I'm the orphan. I'm not going to save the relationship
                        }
                        continue;

                    case RelationType.ManyToOne:

                       
                        // Console.WriteLine($"Handling relationship: {prop.Name} {relationshipAttribute.Type}, From: {relationshipAttribute.RelatedType} To: {entity.GetType().Name}");
                        // Store the foreign key value
                        // get key attribute of the related entity
                        var foreignKeyProp = HelperFuncs.GetKeyProperty(relationshipAttribute.RelatedType);
                        //console.WriteLine($"foreign entity type: {relAttr.RelatedType.Name}");
                        //console.WriteLine($"Foreign key property: {foreignKeyProp.Name}");
                        //console.WriteLine($"Foreign key value: {foreignKeyProp.GetValue(prop.GetValue(entity))?.ToString()}");

                        if (foreignKeyProp != null)
                        {
                            // gets the keyattribute value of the related entity3
                            var relatedEntity = prop.GetValue(entity) as Entity;
                            if (relatedEntity == null)
                            {
                                Console.WriteLine($"Related entity is null for property: {prop.Name}");
                                continue;
                            }
                            var foreignKeyValue = foreignKeyProp.GetValue(relatedEntity)?.ToString()!;
                            // type and foreign key property name
                            var elementName = HelperFuncs.GetForeignKeyElementName(relationshipAttribute.RelatedType, foreignKeyProp);
                            // Console.WriteLine($"elementName: {elementName}");
                            entityElement.Add(new XElement(elementName, foreignKeyValue?.ToString() ?? ""));

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
                var sourceEntityType = entity.GetType();
                var mappingFilePath = Path.Combine(_basePath, relationshipFile);
                var mappingDocument = GetOrCreateDocument(relationshipFile, mappingFilePath);
                var mappingRoot = mappingDocument.Root;
                var sourceEntityKeyProperty = HelperFuncs.GetKeyProperty(sourceEntityType);
                var sourceEntityKeyValue = sourceEntityKeyProperty.GetValue(entity)?.ToString();

                // Remove existing relationships for this entity
                var existingRelationships = mappingRoot.Elements("Relationship")
                    .Where(e => e.Element(sourceEntityType.Name)?.Value == sourceEntityKeyValue)
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
                    if (relatedKeyProp == null)
                    {
                        continue;
                    };

                    var relatedKeyValue = relatedKeyProp.GetValue(relatedEntity)?.ToString();

                    var relationshipElement = new XElement("Relationship",
                        new XElement(sourceEntityType.Name, sourceEntityKeyValue),
                        new XElement(relatedType.Name, relatedKeyValue)
                    );
                    mappingRoot.Add(relationshipElement);
                }

                mappingDocument.Save(mappingFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving relationship mapping: {ex.Message}");
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
                var relAttrType = relAttr?.RelatedType;
                var entityInstance = Activator.CreateInstance(relAttrType!)!;

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
                        break;
                    case RelationType.OneToOne: // TODO BIG TODO - 
                        if (relAttr.OnDelete == DeleteBehavior.Orphan)
                        {
                            break;
                        }
                        HandleOneToOneDeletion(entity, relAttr, entityKeyValue);
                        break;
                }
            }
        }

        private void HandleOneToOneDeletion<T>(T entity, RelationshipAttribute relAttr, string entityKeyValue) where T : Entity
        {
            // Get the XML file for the related entities
            var relatedTableName = relAttr.RelatedType.Name;
            var relatedXmlPath = Path.Combine(_basePath, $"{relatedTableName}.xml");

            if (!File.Exists(relatedXmlPath)) return;

            var doc = XDocument.Load(relatedXmlPath);
            var root = doc.Root;

            // Get the foreign key property name
            var relationProperty = $"{entity.GetType().Name}_{HelperFuncs.GetKeyProperty(relAttr.RelatedType).Name}";

            // Find the related entity that references this entity
            var relatedEntityXml = root.Elements("Entity")
                .FirstOrDefault(e => e.Element(relationProperty)?.Value == entityKeyValue);

            if (relatedEntityXml == null) return;

            var relatedEntity = (Entity)HelperFuncs.XmlToEntity(relatedEntityXml, relAttr.RelatedType);
            
            switch (relAttr.OnDelete)
            {
                case DeleteBehavior.Cascade:
                    // Delete the related entity
                    relatedEntity.IsDeleted = true;
                    HelperFuncs.TrackDeletedEntity(relatedEntity);
                    DeleteEntity(relatedEntity, relatedTableName);
                    break;

                case DeleteBehavior.SetNull:
                    // Remove the foreign key reference
                    var foreignKeyElement = relatedEntityXml.Element(relationProperty);
                    foreignKeyElement?.Remove();
                    doc.Save(relatedXmlPath);
                    break;
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

            // First collect all relationships and related entities to process
            var toProcess = new List<(PropertyInfo prop, Entity relatedEntity, Type relatedType, DeleteBehavior ondelete)>();

            var relationshipsToRemove = rootElement.Elements("Relationship")
                .Where(e => e.Element(entityType.Name)?.Value == entityKeyValue)
                .ToList();

            // First pass - collect everything we need to process
            foreach (var rel in relationshipsToRemove)
            {
                var relatedKey = rel.Element(relatedType.Name)?.Value;
                var relatedAType = Assembly.GetExecutingAssembly().GetTypes()
                    .FirstOrDefault(t => t.Name == relatedType.Name);

                if (relatedAType != null)
                {
                    /* gets the related entity by its key */
                    var relatedEntity = (Entity)HelperFuncs.LoadEntityByKey(relatedAType, relatedKey);
                    if (relatedEntity != null)
                    {
                        var prop = relatedEntity.GetType().GetProperties()
                            .First(p => p.GetCustomAttribute<RelationshipAttribute>() != null &&
                                        p.GetCustomAttribute<RelationshipAttribute>()?.RelatedType == entityType);
                        var ondelete = prop.GetCustomAttribute<RelationshipAttribute>()?.OnDelete ?? DeleteBehavior.Restrict;

                        // BIG TODO - check on delete behavior
                        toProcess.Add((prop, relatedEntity, relatedAType, ondelete));
                    }
                }

                rel.Remove();
            }

            // Second pass - process relationships


            // Save relationship changes
            doc.Save(filePath);

            // Finally, process entity deletions
            foreach (var (prop, relatedEntity, relatedAType, deleteBehavior) in toProcess)
            {
                // BIG TODO - check on delete behavior
                switch (deleteBehavior)
                {
                    case DeleteBehavior.Cascade:
                        relatedEntity.IsDeleted = true;
                        HelperFuncs.TrackDeletedEntity(relatedEntity);
                        DeleteEntity(relatedEntity, relatedAType.Name);
                        break;
                    case DeleteBehavior.Restrict:
                    case DeleteBehavior.None:
                        break;
                }
            }
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
            Console.WriteLine($"Handling One to Many deletion");
            // Get the XML file for the related entities
            var relatedTableName = relAttr.RelatedType.Name;
            var relatedXmlPath = Path.Combine(_basePath, $"{relatedTableName}.xml");

            if (!File.Exists(relatedXmlPath)) return;

            var doc = XDocument.Load(relatedXmlPath);
            var root = doc.Root;

            var relationProperty = $"{entity.GetType().Name}_{HelperFuncs.GetKeyProperty(relAttr.RelatedType).Name}";

            // Find all related entities that reference this entity
            var relatedEntities = root.Elements("Entity")
                .Where(e => e.Element(relationProperty)?.Value == parentKeyValue)
                .ToList();

            var toProcess = new List<( Entity relatedEntity, Type relatedType, DeleteBehavior ondelete)>();

            // Remove or update the related entities based on your business logic
            foreach (var relatedEntityXML in relatedEntities)
            {
                // check if foreign key property is required
                // check if the foreign key property is required
                var relatedEntity = (Entity)HelperFuncs.XmlToEntity(relatedEntityXML, relAttr.RelatedType);
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
                            Console.WriteLine($"deleting entity: {relatedEntity.GetType().Name}, ");
                            // BIG TODO - add to toProcess list
                            toProcess.Add((relatedEntity, relatedEntity.GetType(), relAttr.OnDelete));
                            break;
                        case DeleteBehavior.Restrict:
                            // do nothing
                            break;
                    }
                }
            }
            doc.Save(relatedXmlPath);

            foreach (var (relatedEntity, relatedType, deleteBehavior) in toProcess)
            {
                switch (deleteBehavior)
                {
                    case DeleteBehavior.Cascade:
                        
                        relatedEntity.IsDeleted = true;
                        HelperFuncs.TrackDeletedEntity(relatedEntity);
                        DeleteEntity(relatedEntity, relatedEntity.GetType().Name);
                        break;
                }
            }
        }

        public void DeleteOrphans()
        {
            Console.WriteLine("Deleting orphans...");
            // Get all entity types from the executing assembly
            var entityTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Entity)));

            foreach (var entityType in entityTypes)
            {
                Console.WriteLine($"Deleting orphans for entity type: {entityType.Name}");
                // Find properties with OneToOne relationships that have Orphan delete behavior
                var orphanableProperties = entityType.GetProperties()
                    .Where(p => {
                        var relAttr = p.GetCustomAttribute<RelationshipAttribute>();
                        return relAttr?.Type == RelationType.OneToOne && 
                               relAttr.OnDelete == DeleteBehavior.Orphan; // if its cascade then the other side must hold orphan. otherwise it's circular reference
                    });

                if (!orphanableProperties.Any()) continue;

                var xmlPath = HelperFuncs.GetTablePath(_basePath, entityType.Name);
                if (!File.Exists(xmlPath)) continue;

                var doc = XDocument.Load(xmlPath);
                var root = doc.Root;
                var entitiesToRemove = new List<XElement>();

                foreach (var entityElement in root.Elements("Entity"))
                {
                    foreach (var prop in orphanableProperties)
                    {
                        Console.WriteLine($"Checking property: {prop.Name}");
                        var relAttr = prop.GetCustomAttribute<RelationshipAttribute>();
                        var relatedType = relAttr.RelatedType;
                        
                        // Get the foreign key element name
                        var foreignKeyProp = HelperFuncs.GetKeyProperty(relatedType);
                        var elementName = HelperFuncs.GetForeignKeyElementName(relatedType, foreignKeyProp);
                        
                        // Check if the related entity exists
                        var foreignKeyValue = entityElement.Element(elementName)?.Value;
                        if (foreignKeyValue != null)
                        {
                            Console.WriteLine($"Foreign key value: {foreignKeyValue}");
                            // Check if the related entity is deleted
                            if (HelperFuncs.IsEntityDeleted(relatedType, foreignKeyValue))
                            {
                                Console.WriteLine($"Related entity is deleted: {foreignKeyValue}");
                                entitiesToRemove.Add(entityElement);
                                break; // No need to check other properties if we're already removing this entity
                            }
                        }
                    }
                }

                // Remove orphaned entities
                foreach (var element in entitiesToRemove)
                {
                    Console.WriteLine($"Removing orphaned entity: {element}");
                    element.Remove();
                }

                if (entitiesToRemove.Any())
                {
                    doc.Save(xmlPath);
                }
            }
        }





        // public IQueryable<T> Query<T>() where T : Entity
        // {
        //     var builder = new XmlQueryBuilder<T>(_basePath, typeof(T).Name);
        //     return builder.Execute().AsQueryable();
        // }
    }


}
