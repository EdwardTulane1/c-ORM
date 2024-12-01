using MyORM.Attributes;
using System.Reflection;
using MyORM.Core;


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


    }
}