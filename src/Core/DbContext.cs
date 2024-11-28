/*
 * DbContext serves as the main entry point for interacting with the database/storage.
 * It manages entity sets (tables), tracks changes, and handles saving data to storage.
 * This implementation specifically works with XML storage but follows common ORM patterns.
 */

using System.Data;
using System.Reflection;
using MyORM.Attributes;
using MyORM.Attributes.Validation;
using MyORM.Helper;

namespace MyORM.Core
{
    public abstract class DbContext : IDisposable
    {
        private readonly string _xmlBasePath;
        private readonly XmlStorageProvider _storageProvider;
        private Dictionary<Type, HashSet<string>> _deletedEntities;
        protected Dictionary<Type, string> TableMappings { get; } = new Dictionary<Type, string>();

        /*
         * Constructor initializes the context with the XML storage path
         * Sets up the storage provider and initializes all DbSet properties
         * @param xmlBasePath: Base path where XML files will be stored
         */
        protected DbContext(string xmlBasePath)
        {
            _xmlBasePath = xmlBasePath;
            _storageProvider = new XmlStorageProvider(xmlBasePath);
            _deletedEntities = new Dictionary<Type, HashSet<string>>();
            InitializeDbSets();
            MapEntities();
        }

        /*
         * Initializes all DbSet properties in the context
         * Uses reflection to find properties of type DbSet<T>
         * Creates instances of DbSets for each entity type
         */
        private void InitializeDbSets()
        {
            var dbSetProperties = GetType().GetProperties()
                .Where(p => p.PropertyType.IsGenericType &&
                           p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

            foreach (var property in dbSetProperties)
            {
                var entityType = property.PropertyType.GetGenericArguments()[0];
                var dbSetType = typeof(DbSet<>).MakeGenericType(entityType);
                var dbSet = Activator.CreateInstance(dbSetType, new object[] { this });
                property.SetValue(this, dbSet);
            }
        }

        /*
         * Maps entity types to their table names using attributes
         * Reads TableAttribute from entity classes to determine table names
         * Stores mappings in TableMappings dictionary
         */
        private void MapEntities()
        {
            var entityTypes = GetType().GetProperties()
                .Where(p => p.PropertyType.IsGenericType &&
                           p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .Select(p => p.PropertyType.GetGenericArguments()[0]);

            foreach (var type in entityTypes)
            {
                var tableAttr = type.GetCustomAttribute<TableAttribute>();
                if (tableAttr != null)
                {
                    TableMappings[type] = tableAttr.TableName;
                }
            }
        }

        /*
         * Saves all pending changes to storage
         * Processes all modified, new, and deleted entities
         * Validates entities before saving
         * Updates entity states after successful save
         */
        public void SaveChanges()
        {
            // Clear previous tracking
            _deletedEntities.Clear();

            var graph = BuildDependencyGraph();
            var sortedEntityTypes = graph.GetSortedEntities();

            // First handle deletions in reverse order ( the entites you depends on will be updated first)
            foreach (var entityType in sortedEntityTypes.AsEnumerable().Reverse())
            {
                Console.WriteLine($"Saving entities of type: {entityType.Name}");
                var dbSetProperty = GetType().GetProperties()
                    .First(p => p.PropertyType == typeof(DbSet<>).MakeGenericType(entityType));
                var dbSet = dbSetProperty.GetValue(this);
                Console.WriteLine($"DbSet property: {dbSetProperty.Name}");
                
                var entities = ((IEnumerable<Entity>)dbSet
                    .GetType()
                    .GetField("_entities", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(dbSet))
                    .ToList();

                foreach (var entity in entities)
                {
                    if (entity.IsDeleted)
                    {
                        HelperFuncs.TrackDeletedEntity(entity);
                        _storageProvider.DeleteEntity(entity, entityType.Name);
                    }
                    else
                    {
                        ValidationHelper.ValidateEntity(entity);
                        _storageProvider.SaveEntity(entity, entityType.Name);
                        entity.IsNew = false;
                        entity.IsModified = false;
                        entity.TakeSnapshot();
                    }
                }
                HelperFuncs.ClearDeletedEntities();
            }
        }

        private DependencyGraph BuildDependencyGraph()
        {
            var graph = new DependencyGraph();

            foreach (var dbSetProperty in GetType().GetProperties()
                .Where(p => p.PropertyType.IsGenericType &&
                            p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)))
            {
                var entityType = dbSetProperty.PropertyType.GetGenericArguments()[0];
                graph.AddNode(entityType);

                // Add dependencies based on relationships
                var relationshipProps = entityType.GetProperties()
                    .Where(p => p.GetCustomAttribute<RelationshipAttribute>() != null);

                foreach (var prop in relationshipProps)
                {
                    var relAttr = prop.GetCustomAttribute<RelationshipAttribute>();
                    if(relAttr == null) continue;
                    switch (relAttr.Type)
                    {
                        case RelationType.ManyToOne:
                        case RelationType.OneToOne:
                            // Add dependency from current entity to related entity
                            graph.AddDependency(entityType, relAttr.RelatedType);
                            break;
                        case RelationType.OneToMany:
                            // Add dependency from related entity to current entity
                            graph.AddDependency(relAttr.RelatedType, entityType);
                            break;
                        case RelationType.ManyToMany:
                            // For many-to-many, we don't add direct dependencies
                            break;
                    }
                }
            }
            return graph;
        }

        
        public void Dispose()
        {
            // TODO: ... 
        }
    }
}
