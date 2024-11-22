using System;
using System.Reflection;


using MyORM.Attributes.Validation;
using MyORM.Attributes;


namespace MyORM.Core
{
    public abstract class Entity 
    {
        internal Dictionary<string, object> OriginalValues { get; } = new();
        
        public virtual bool IsNew { get; internal set; } = true;
        public virtual bool IsModified { get; internal set; }
        public virtual bool IsDeleted { get; internal set; }

        internal void TakeSnapshot()
        {
            Console.WriteLine("Taking snapshot");
            OriginalValues.Clear();
            var properties = GetType().GetProperties()
                .Where(p => p.GetCustomAttribute<ColumnAttribute>() != null || 
                           p.GetCustomAttribute<KeyAttribute>() != null);
            Console.WriteLine("Properties: " + string.Join(", ", properties.Select(p => p.Name)));  
            foreach (var prop in properties)
            {
                Console.WriteLine($"Property: {prop.Name}, Value: {prop.GetValue(this)}");
                OriginalValues[prop.Name] = prop.GetValue(this);
            }
            // print snapshot
        }

        internal bool HasChanges()
        {
            if (IsNew || IsDeleted) return true;
            
            var properties = GetType().GetProperties()
                .Where(p => p.GetCustomAttribute<ColumnAttribute>() != null || 
                           p.GetCustomAttribute<KeyAttribute>() != null);
 
            // print snapshot
            Console.WriteLine("Snapshot: " + string.Join(", ", OriginalValues.Select(kv => $"{kv.Key}: {kv.Value}")));
            // print current values
            Console.WriteLine("Current: " + string.Join(", ", properties.Select(p => $"{p.Name}: {p.GetValue(this)}")));
            foreach (var prop in properties)
            {
                var currentValue = prop.GetValue(this);
                if (!OriginalValues.ContainsKey(prop.Name) || 
                    !Equals(OriginalValues[prop.Name], currentValue))
                {
                    return true;
                }
            }
            Console.WriteLine("No changes");
            return false;
        }

    }
}
