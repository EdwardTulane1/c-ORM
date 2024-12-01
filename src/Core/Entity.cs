/*
 * Entity is the base class for all database entities in the ORM.
 * It provides change tracking functionality by maintaining original values
 * and state flags (IsNew, IsModified, IsDeleted).
 * Only properties marked with Column or Key attributes are tracked.
 */

using System.Reflection;
using MyORM.Attributes;

namespace MyORM.Core
{
    public abstract class Entity 
    {
        /*
         * Stores the original values of tracked properties
         * Used to detect changes by comparing against current values
         */
        internal Dictionary<string, object> OriginalValues { get; } = new();
        
        /*
         * State flags to track entity status
         * IsNew: True for newly created entities that haven't been saved
         * IsModified: True when tracked properties have changed
         * IsDeleted: True when entity is marked for deletion
         */
        public virtual bool IsNew { get; internal set; } = true;
        public virtual bool IsModified { get; internal set; }
        public virtual bool IsDeleted { get; internal set; }

        /*
         * Takes a snapshot of current property values
         * Stores values of properties marked with Column or Key attributes
         * Used as baseline for detecting changes
         */
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
                OriginalValues[prop.Name] = prop.GetValue(this)!;
            }
        }

        /*
         * Checks if entity has any changes by comparing current values with snapshot
         * Returns true if entity is new, deleted, or has modified properties
         * Only considers properties marked with Column or Key attributes
         * @returns: boolean indicating whether entity has changes
         */
        internal bool HasChanges()
        {
            if (IsNew || IsDeleted) return true;
            
            var properties = GetType().GetProperties()
                .Where(p => p.GetCustomAttribute<ColumnAttribute>() != null || 
                           p.GetCustomAttribute<KeyAttribute>() != null);
 
            Console.WriteLine("Snapshot: " + string.Join(", ", OriginalValues.Select(kv => $"{kv.Key}: {kv.Value}")));
            Console.WriteLine("Current: " + string.Join(", ", properties.Select(p => $"{p.Name}: {p.GetValue(this)}")));
            
            foreach (var prop in properties)
            {
                var currentValue = prop.GetValue(this);
                if (!OriginalValues.ContainsKey(prop.Name) || 
                    !Equals(OriginalValues[prop.Name], currentValue))
                {

                    IsModified = true;
                    return true;
                }
            }
            Console.WriteLine("No changes");
            return false;
        }
    }
}
