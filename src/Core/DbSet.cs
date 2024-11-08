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

            var entityType = typeof(T);
            var relationshipProps = entityType.GetProperties()
                .Where(p => p.GetCustomAttribute<RelationshipAttribute>() != null);

            foreach (var prop in relationshipProps)
            {
                var relAttr = prop.GetCustomAttribute<RelationshipAttribute>();
                if (relAttr.Type == RelationType.ManyToOne)
                {
                    var relatedEntity = prop.GetValue(entity) as Entity;
                    if (relatedEntity != null)
                    {
                        var foreignKeyProp = entityType.GetProperty(relAttr.FromProperty);
                        var relatedKeyProp = relatedEntity.GetType().GetProperty(relAttr.ToProperty);
                        
                        if (foreignKeyProp != null && relatedKeyProp != null)
                        {
                            var relatedKeyValue = relatedKeyProp.GetValue(relatedEntity);
                            foreignKeyProp.SetValue(entity, relatedKeyValue);
                        }
                    }
                }
            }
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
