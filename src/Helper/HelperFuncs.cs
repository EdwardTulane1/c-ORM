using MyORM.Attributes;
using System.Reflection;
using MyORM.Core;
using System.IO;
using System.Xml.Linq;

namespace MyORM.Helper
{
    public static class HelperFuncs
    {
        private static Dictionary<Type, HashSet<string>> _deletedEntities = new Dictionary<Type, HashSet<string>>();

        public static PropertyInfo GetKeyProperty(Type type)
        {
            var keyProp = type.GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);

            if (keyProp == null)
            {
                throw new InvalidOperationException($"No key property found for type {type.Name}");
            }

            return keyProp;
        }


        public static void TrackDeletedEntity(Entity entity)
        {
            var entityType = entity.GetType();
            var keyProp = HelperFuncs.GetKeyProperty(entityType);
            var keyValue = keyProp.GetValue(entity)?.ToString();

            if (!_deletedEntities.ContainsKey(entityType))
            {
                _deletedEntities[entityType] = new HashSet<string>();
            }
            _deletedEntities[entityType].Add(keyValue);
        }

        public static string getFileNameAlphaBetic(string type1, string type2)
        {
            var orderedTypes = new[] { type1, type2 }.OrderBy(t => t).ToArray();
            return $"{orderedTypes[0]}_{orderedTypes[1]}.xml";
        }

        public static bool IsEntityDeleted(Type type, string keyValue)
        {
            return _deletedEntities.ContainsKey(type) &&
                       _deletedEntities[type].Contains(keyValue);
        }

        public static void ClearDeletedEntities()
        {
            _deletedEntities.Clear();
        }

        public static string GetTablePath(string basePath, string tableName)
        {
            return Path.Combine(basePath, $"{tableName}.xml");
        }

        public static T XmlToEntity<T>(XElement element) where T : Entity
        {
            return (T)XmlToEntity(element, typeof(T));
        }

        public static object XmlToEntity(XElement element, Type entityType)
        {
            var entity = Activator.CreateInstance(entityType);
            var properties = entityType.GetProperties();
            
            foreach (var prop in properties)
            {
                if (prop.GetCustomAttribute<ColumnAttribute>() != null || 
                    prop.GetCustomAttribute<KeyAttribute>() != null)
                {
                    var value = element.Element(prop.Name)?.Value;
                    if (value != null)
                    {
                        var convertedValue = Convert.ChangeType(value, prop.PropertyType);
                        prop.SetValue(entity, convertedValue);
                    }
                }
            }
            return entity;
        }
    }

    
}