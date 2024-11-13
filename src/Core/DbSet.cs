using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MyORM.Attributes;

namespace MyORM.Core
{
    public class DbSet<T> where T : Entity
    {
        private readonly DbContext _context;
        private readonly List<T> _entities = new List<T>();

        public DbSet(DbContext context)
        {
            _context = context;
            _entities = new List<T>();
        }

        public void Add(T entity)
        {
            _entities.Add(entity);
        }

        public void Remove(T entity)
        {
            _entities.Remove(entity);
            entity.IsDeleted = true;
        }

        public IQueryable<T> AsQueryable()
        {
            return _entities.AsQueryable();
        }
    }
}
