/*
 * DbContext serves as the main entry point for interacting with the database/storage.
 * It manages entity sets (tables), tracks changes, and handles saving data to storage.
 * This implementation specifically works with XML storage but follows common ORM patterns.
 */

using System.Data;
using System.Reflection;
using MyORM.Attributes;
using MyORM.Attributes.Validation;

namespace MyORM.Core
{
    public abstract class DbContext : IDisposable
    {
        private readonly XmlStorageProvider _storageProvider;
        private readonly XmlConnection _connection;
        protected Dictionary<Type, string> TableMappings { get; } = new Dictionary<Type, string>();


        /*
         * Constructor initializes the context with the XML storage path
         * Sets up the storage provider and initializes all DbSet properties
         * @param xmlBasePath: Base path where XML files will be stored
         */
        protected DbContext()
        {
            _connection = XmlConnection.Instance;
            _connection.Open();
            _storageProvider = new XmlStorageProvider(_connection);
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
            var graph = BuildDependencyGraph();
            var sortedEntityTypes = graph.GetSortedEntities();

            foreach (var entityType in sortedEntityTypes.AsEnumerable().Reverse())
            {
                var dbSetProperty = GetType().GetProperties()
                   .First(p => p.PropertyType == typeof(DbSet<>).MakeGenericType(entityType));
                var dbSet = dbSetProperty.GetValue(this);

                var entities = ((IEnumerable<Entity>)dbSet
                    .GetType()
                    .GetField("_entities", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(dbSet))
                    .ToList();

                foreach (var entity in entities)
                {
                  
                    if (entity.IsDeleted)
                    {
                        _storageProvider.DeleteEntity(entity, entityType.Name);
                    }
                    else if (entity.HasChanges())
                    {
                        ValidationHelper.ValidateEntity(entity, _connection);
                        _storageProvider.SaveEntity(entity, entityType.Name);
                        entity.IsNew = false;
                        entity.IsModified = false;
                        entity.TakeSnapshot();
                    }
                }
                _storageProvider.DeleteOrphans();
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
                    if (relAttr == null) continue;
                    switch (relAttr.Type)
                    {
                        case RelationType.OneToMany:
                            graph.AddDependency(relAttr.RelatedType, entityType);
                            break;
                        case RelationType.OneToOne:
                            if(relAttr.OnDelete == DeleteBehavior.Cascade || relAttr.OnDelete == DeleteBehavior.SetNull){ // One of the sides must be depend on another while the pther one doesnt
                                graph.AddDependency(relAttr.RelatedType, entityType);
                            }
                            break;
                        case RelationType.ManyToOne: 
                            graph.AddDependency(entityType, relAttr.RelatedType);
                            break;
                        case RelationType.ManyToMany:
                            if(relAttr.OnDelete == DeleteBehavior.Cascade){ 
                                graph.AddDependency(relAttr.RelatedType, entityType);
                            }
                            break;
                    }
                }
            }
            return graph;
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }

        protected DbSet<T> GetDbSet<T>() where T : Entity
        {
            var dbSet = GetType().GetProperties()
                .First(p => p.PropertyType == typeof(DbSet<>).MakeGenericType(typeof(T)));
            return (DbSet<T>)dbSet.GetValue(this);
        }

        public XmlQueryBuilder<T> Query<T>() where T : Entity
        {
            var dbSet = GetDbSet<T>();
            return new XmlQueryBuilder<T>(GetConnection(), typeof(T).Name, dbSet);
        }

        internal XmlConnection GetConnection()
        {
            return XmlConnection.Instance;
        }
    }
}

