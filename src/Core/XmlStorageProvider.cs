using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.IO;
using System.Linq;
using System.Reflection;
using MyORM.Attributes;

namespace MyORM.Core
{
    public class XmlStorageProvider
    {
        private readonly string _basePath;
        private readonly Dictionary<string, XDocument> _documents;

        public XmlStorageProvider(string basePath)
        {
            _basePath = basePath;
            _documents = new Dictionary<string, XDocument>();
            
            Console.WriteLine($"Ensuring directory exists: {basePath}");
            Directory.CreateDirectory(basePath);
        }

        public void SaveEntity<T>(T entity, string tableName) where T : Entity
        {
            var xmlPath = Path.Combine(_basePath, $"{tableName}.xml");
            Console.WriteLine($"Saving entity to: {xmlPath}");
            
            var doc = GetOrCreateDocument(tableName, xmlPath);
            var root = doc.Root;

            var entityElement = new XElement("Entity");
            
            // Debug all properties - Get properties from the actual type, not just Entity
            var actualType = entity.GetType();  // This gets the real type (Customer or Order)
            Console.WriteLine($"\nDebug info for {actualType.Name}:");
            
            foreach (var prop in actualType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
                var keyAttr = prop.GetCustomAttribute<KeyAttribute>();
                var value = prop.GetValue(entity);
                
                Console.WriteLine($"Property: {prop.Name}");
                Console.WriteLine($"  Value: {value}");
                Console.WriteLine($"  Column Attribute: {columnAttr?.ColumnName ?? "none"}");
                Console.WriteLine($"  Key Attribute: {(keyAttr != null ? "yes" : "no")}");
                
                if (columnAttr != null || keyAttr != null)
                {
                    entityElement.Add(new XElement(prop.Name, value?.ToString() ?? ""));
                }
            }

            // Find existing entity or add new one
            var keyProp = actualType.GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);

            if (keyProp != null)
            {
                var keyValue = keyProp.GetValue(entity)?.ToString();
                var existingEntity = root.Elements("Entity")
                    .FirstOrDefault(e => e.Element(keyProp.Name)?.Value == keyValue);

                if (existingEntity != null)
                {
                    existingEntity.ReplaceWith(entityElement);
                }
                else
                {
                    root.Add(entityElement);
                }
            }
            else
            {
                root.Add(entityElement);
            }

            doc.Save(xmlPath);
            Console.WriteLine($"Saved to {xmlPath}");
        }

        public void DeleteEntity<T>(T entity, string tableName) where T : Entity
        {
            var xmlPath = Path.Combine(_basePath, $"{tableName}.xml");
            var doc = GetOrCreateDocument(tableName, xmlPath);
            var root = doc.Root;

            var keyProp = typeof(T).GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);
            
            if (keyProp != null)
            {
                var keyValue = keyProp.GetValue(entity)?.ToString();
                var elementToRemove = root.Elements("Entity")
                    .FirstOrDefault(e => e.Element(keyProp.Name)?.Value == keyValue);
                
                elementToRemove?.Remove();
                doc.Save(xmlPath);
            }
        }

        private XDocument GetOrCreateDocument(string tableName, string xmlPath)
        {
            if (_documents.ContainsKey(tableName))
            {
                return _documents[tableName];
            }

            XDocument doc;
            if (File.Exists(xmlPath))
            {
                doc = XDocument.Load(xmlPath);
            }
            else
            {
                doc = new XDocument(new XElement("Entities"));
            }

            _documents[tableName] = doc;
            return doc;
        }
    }
}
