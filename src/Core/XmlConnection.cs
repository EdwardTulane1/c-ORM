using System.Xml.Linq;

namespace MyORM.Core
{
    /// <summary>
    /// Handles low-level XML file operations and connection management
    /// </summary>
    public class XmlConnection : IDisposable
    {
        public readonly string _basePath;
        private readonly Dictionary<string, XDocument> _documentCache;
        private bool _isOpen;
        private readonly object _lockObject = new object();

        public XmlConnection(string basePath)
        {
            _basePath = basePath;
            _documentCache = new Dictionary<string, XDocument>();
            _isOpen = false;
            
            // Ensure storage directory exists
            Directory.CreateDirectory(basePath);
        }

        /// <summary>
        /// Opens the connection and initializes resources
        /// </summary>
        public void Open()
        {
            lock (_lockObject)
            {
                if (_isOpen)
                    throw new InvalidOperationException("Connection is already open");
                
                _isOpen = true;
            }
        }

        /// <summary>
        /// Closes the connection and flushes any pending changes
        /// </summary>
        public void Close()
        {
            lock (_lockObject)
            {
                if (!_isOpen)
                    return;

                // Flush all cached documents
                foreach (var doc in _documentCache)
                {
                    FlushDocument(doc.Value, doc.Key);
                }
                
                _documentCache.Clear();
                _isOpen = false;
            }
        }

        /// <summary>
        /// Gets or creates an XML document for the specified table
        /// </summary>
        public XDocument? GetDocument(string tableName, bool createIfNotExists = true)
        {
            EnsureConnectionOpen();

            lock (_lockObject)
            {
                if (_documentCache.TryGetValue(tableName, out var cachedDoc))
                    return cachedDoc;

                var path = GetTablePath(tableName);
                var doc = File.Exists(path)
                    ? XDocument.Load(path)
                    : createIfNotExists ? CreateNewDocument(tableName) : null;

                if (doc != null)
                {
                    _documentCache[tableName] = doc;
                }
                return doc;
            }
        }

        /// <summary>
        /// Saves changes to a specific document
        /// </summary>
        public void SaveDocument(string tableName)
        {
            EnsureConnectionOpen();

            lock (_lockObject)
            {
                if (_documentCache.TryGetValue(tableName, out var doc))
                {
                    FlushDocument(doc, tableName);
                }
            }
        }

        /// <summary>
        /// Deletes a table's XML file
        /// </summary>
        public void DeleteTable(string tableName)
        {
            EnsureConnectionOpen();

            lock (_lockObject)
            {
                var path = GetTablePath(tableName);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                _documentCache.Remove(tableName);
            }
        }

        /// <summary>
        /// Creates a backup of a table's XML file
        /// </summary>
        public void BackupTable(string tableName)
        {
            EnsureConnectionOpen();

            var sourcePath = GetTablePath(tableName);
            var backupPath = $"{sourcePath}.bak";

            lock (_lockObject)
            {
                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, backupPath, true);
                }
            }
        }

        /// <summary>
        /// Restores a table's XML file from backup
        /// </summary>
        public bool RestoreTable(string tableName)
        {
            EnsureConnectionOpen();

            var sourcePath = GetTablePath(tableName);
            var backupPath = $"{sourcePath}.bak";

            lock (_lockObject)
            {
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, sourcePath, true);
                    _documentCache.Remove(tableName);
                    return true;
                }
                return false;
            }
        }

        private void FlushDocument(XDocument doc, string tableName)
        {
            var path = GetTablePath(tableName);
            doc.Save(path);
        }

        private XDocument CreateNewDocument(string tableName)
        {
            return new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("Table", 
                    new XAttribute("name", tableName))
            );
        }

        private string GetTablePath(string tableName)
        {
            return Path.Combine(_basePath, $"{tableName}.xml");
        }

        private void EnsureConnectionOpen()
        {
            if (!_isOpen)
                throw new InvalidOperationException("Connection is not open");
        }

        public void Dispose()
        {
            Close();
        }
    }
} 