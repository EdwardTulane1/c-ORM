using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using MyORM.Attributes;

namespace MyORM.Core
{
    public abstract class DbContext : IDisposable
    {
        private readonly string _xmlBasePath;
        private readonly XmlStorageProvider _storageProvider;
        protected Dictionary<Type, string> TableMappings { get; } = new Dictionary<Type, string>();

        protected DbContext(string xmlBasePath)
        {
            _xmlBasePath = xmlBasePath;
            _storageProvider = new XmlStorageProvider(xmlBasePath);
            InitializeDbSets();
            MapEntities();
        }

        private void InitializeDbSets()
        {
            var dbSetProperties = GetType().GetProperties()
                .Where(p => p.PropertyType.IsGenericType &&
                           p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

            foreach (var property in dbSetProperties)
            {
                // Create DbSet instance for each that needs to be initialized
                var entityType = property.PropertyType.GetGenericArguments()[0];
                var dbSetType = typeof(DbSet<>).MakeGenericType(entityType);
                var dbSet = Activator.CreateInstance(dbSetType, new object[] { this });
                property.SetValue(this, dbSet);
            }
        }

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

        public void SaveChanges()
        {
            foreach (var mapping in TableMappings)
            {
                var entityType = mapping.Key;
                var tableName = mapping.Value;
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
                        _storageProvider.DeleteEntity(entity, tableName);
                    }
                    else 
                    {
                        _storageProvider.SaveEntity(entity, tableName);
                        entity.IsNew = false;
                        entity.IsModified = false;
                    }
                }
            }
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}
