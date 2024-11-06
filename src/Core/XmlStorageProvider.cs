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
            Directory.CreateDirectory(basePath);
        }

        public void SaveEntity<T>(T entity, string tableName) where T : Entity
        {
            var xmlPath = Path.Combine(_basePath, $"{tableName}.xml");
            var doc = GetOrCreateDocument(tableName, xmlPath);
            var root = doc.Root;

            var entityElement = new XElement("Entity");
            var properties = typeof(T).GetProperties()
                .Where(p => p.GetCustomAttribute<ColumnAttribute>() != null || 
                           p.GetCustomAttribute<KeyAttribute>() != null);

            foreach (var prop in properties)
            {
                var value = prop.GetValue(entity)?.ToString() ?? "";
                entityElement.Add(new XElement(prop.Name, value));
            }

            if (entity.IsNew)
            {
                root.Add(entityElement);
            }
            else if (entity.IsModified)
            {
                var keyProp = typeof(T).GetProperties()
                    .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);
                
                if (keyProp != null)
                {
                    var keyValue = keyProp.GetValue(entity).ToString();
                    var existingElement = root.Elements("Entity")
                        .FirstOrDefault(e => e.Element(keyProp.Name)?.Value == keyValue);
                    
                    if (existingElement != null)
                    {
                        existingElement.ReplaceWith(entityElement);
                    }
                }
            }

            doc.Save(xmlPath);
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
                var keyValue = keyProp.GetValue(entity).ToString();
                var elementToRemove = root.Elements("Entity")
                    .FirstOrDefault(e => e.Element(keyProp.Name)?.Value == keyValue);
                
                if (elementToRemove != null)
                {
                    elementToRemove.Remove();
                    doc.Save(xmlPath);
                }
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
