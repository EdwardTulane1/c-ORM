/*
 * DbSet<T> represents a collection of entities of a specific type (same table) in the database.
 * It provides methods to manipulate entities (add, remove) and query them.
 * Each DbSet instance tracks changes to its entities and maintains their state.
 */


using System.Reflection;
using MyORM.Attributes;

namespace MyORM.Core
{
    public class DbSet<T> where T : Entity
    {
        private readonly DbContext _context;
        private readonly List<T> _entities = new List<T>();

        /*
         * Constructor initializes a new DbSet with a reference to its parent context
         * @param context: The DbContext that owns this DbSet
         */
        public DbSet(DbContext context)
        {
            _context = context;
            _entities = new List<T>();
        }

        /*
         * Adds a new entity to the DbSet
         * Marks the entity as new and takes a snapshot of its initial state
         * @param entity: The entity to add to the collection
         */
        public void Add(T entity)
        {
            _entities.Add(entity);
            entity.IsNew = true;
            entity.TakeSnapshot();
        }

        /*
         * Removes an entity from the DbSet
         * Marks the entity as deleted rather than immediately removing it (will be removed when SaveChanges is called)
         * @param entity: The entity to mark for deletion
         */
        public void Remove(T entity)
        {
            if (!_entities.Contains(entity))
            {
                _entities.Add(entity);
            }
            entity.IsDeleted = true;
        }

        public void TrackEntity(T entity, bool isNested = false)
        {
            if (!_entities.Contains(entity))
            {
                entity.TakeSnapshot();
                _entities.Add(entity);
                entity.IsNew = false;
                entity.IsModified = false;
                entity.IsDeleted = false;
                if(!isNested)
                {
                    foreach (var property in entity.GetType().GetProperties())
                    {
                        var relationshipAttribute = property.GetCustomAttribute<RelationshipAttribute>();
                        if(relationshipAttribute == null || property.GetValue(entity) == null) continue;
                        TrackEntityB(property, relationshipAttribute, entity);
                    }
                }
            }
            else{
                // Console.WriteLine($"Entity already tracked: {entity.GetType().Name}");
            }
        }
        
        public void TrackEntityB(PropertyInfo propertyInfo, RelationshipAttribute property, Entity entity)
        {
            var value = propertyInfo.GetValue(entity);
            var relatedDbSet = _context.GetType().GetProperties()
                .FirstOrDefault(p => p.PropertyType == typeof(DbSet<>).MakeGenericType(property.RelatedType))
                ?.GetValue(_context);

            if (relatedDbSet == null)
            {
                return;
            }
            switch (property.Type)
            {
                case RelationType.OneToOne:
                case RelationType.ManyToOne:
                    var trackMethod = relatedDbSet.GetType().GetMethod("TrackEntity");
                    trackMethod?.Invoke(relatedDbSet, new[] { value, true });
                    break;
                case RelationType.OneToMany:
                case RelationType.ManyToMany:
                    var collection = value as IEnumerable<object>;
                    if (collection != null)
                    {
                        trackMethod = relatedDbSet.GetType().GetMethod("TrackEntity");
                        foreach (var relatedEntity in collection)
                        {
                            trackMethod?.Invoke(relatedDbSet, new[] { relatedEntity , true});
                        }
                    }
                    break;

            }
        }

        /*
         * Provides LINQ queryable access to the entities
         * Enables filtering, sorting, and other LINQ operations
         * @returns: IQueryable<T> interface for querying entities
         */
        public IQueryable<T> AsQueryable()
        {
            return _entities.AsQueryable();
        }
    }
}
