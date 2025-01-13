using MyORM.Attributes;
using System.Reflection;
using MyORM.Core;
using System.IO;
using System.Xml.Linq;

namespace MyORM.Helper
{
    public static class HelperFuncs
    {
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

        public static string getKey(Type type, Entity entity)
        {
            var keyProp = GetKeyProperty(type);
            return keyProp.GetValue(entity)?.ToString() ?? string.Empty;
        }

        public static string getFileNameAlphaBetic(string type1, string type2)
        {
            var orderedTypes = new[] { type1, type2 }.OrderBy(t => t).ToArray();
            return $"{orderedTypes[0]}_{orderedTypes[1]}.xml";
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


        public static object LoadEntityByKey(Type entityType, string key)
        {
            var xmlPath = HelperFuncs.GetTablePath(
                Path.Combine(Directory.GetCurrentDirectory(), "XmlStorage"),
                entityType.Name);
            if (!File.Exists(xmlPath)) return null;

            var doc = XDocument.Load(xmlPath);
            var keyProp = HelperFuncs.GetKeyProperty(entityType);

            foreach (var element in doc.Root.Elements("Entity"))
            {
                if (element.Element(keyProp.Name)?.Value == key)
                {
                    return HelperFuncs.XmlToEntity(element, entityType);
                }
            }
            return null;
        }

        public static XElement GetEntityByKey(XElement root, PropertyInfo keyProp, string keyValue)
        {
            return root.Elements("Entity")
                .FirstOrDefault(e => e.Element(keyProp.Name)?.Value == keyValue);
        }

        public static IEnumerable<PropertyInfo> GetRelationshipProperties(Type type)
        {
            return type.GetProperties()
                .Where(p => p.GetCustomAttribute<RelationshipAttribute>() != null);
        }

        public static string GetForeignKeyElementName(Type relatedType, PropertyInfo foreignKeyProp)
        {
            return $"{relatedType.Name}_{foreignKeyProp.Name}";
        }

    }
}