/*
 * DbSet<T> represents a collection of entities of a specific type (same table) in the database.
 * It provides methods to manipulate entities (add, remove) and query them.
 * Each DbSet instance tracks changes to its entities and maintains their state.
 */


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
            // _entities.Remove(entity);
            entity.IsDeleted = true;
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
