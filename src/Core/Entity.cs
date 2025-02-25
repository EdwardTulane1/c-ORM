/*
 * Entity is the base class for all database entities in the ORM.
 * It provides change tracking functionality by maintaining original values
 * and state flags (IsNew, IsModified, IsDeleted).
 * Only properties marked with Column or Key attributes are tracked.
 */

using System.Reflection;
using MyORM.Attributes;
using MyORM.Helper;

namespace MyORM.Core
{
    public abstract class Entity
    {
        /*
         * Stores the original values of tracked properties
         * Used to detect changes by comparing against current values
         */
        internal Dictionary<string, object> OriginalValues { get; } = new();
        internal Dictionary<string, string> RelationshipKeys { get; } = new();

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
            OriginalValues.Clear();
            var properties = GetType().GetProperties()
                .Where(p => p.GetCustomAttribute<ColumnAttribute>() != null ||
                           p.GetCustomAttribute<KeyAttribute>() != null);
            foreach (var prop in properties)
            {
                var value = prop.GetValue(this);
                OriginalValues[prop.Name] = value ?? new();
            }

            // Add relationship key snapshot
            var relationshipProps = GetType().GetProperties()
                .Where(p => p.GetCustomAttribute<RelationshipAttribute>() != null);

            foreach (var prop in relationshipProps)
            {
                RelationshipKeys[prop.Name] = getSnapshotRelationValue(prop);
            }
        }


        private string getSnapshotRelationValue(PropertyInfo prop)
        {
            var value = prop.GetValue(this);
            if (value == null) return string.Empty;
            var relationType = prop!.GetCustomAttribute<RelationshipAttribute>()!.Type;
            var relatedEntityType = prop!.GetCustomAttribute<RelationshipAttribute>()!.RelatedType;
            var relatedEntityKey = HelperFuncs.GetKeyProperty(relatedEntityType).Name;
            switch (relationType)
            {
                case RelationType.OneToOne:
                case RelationType.ManyToOne:
                    var key = HelperFuncs.getKey(relatedEntityType, value as Entity);
                    return key;
                case RelationType.ManyToMany:
                    var keys = (value as IEnumerable<Entity>).Select(e => HelperFuncs.getKey(relatedEntityType, e)).OrderBy(k => k).ToArray();
                    return string.Join(",", keys);
                default:
                    return string.Empty;
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
            // Console.WriteLine($"HasChanges: {IsNew}, {IsDeleted}, {IsModified}");
            if (IsNew || IsDeleted || IsModified) return true;

            var properties = GetType().GetProperties()
                .Where(p => p.GetCustomAttribute<ColumnAttribute>() != null ||
                           p.GetCustomAttribute<KeyAttribute>() != null);

            var relationshipProps = GetType().GetProperties()
                .Where(p => p.GetCustomAttribute<RelationshipAttribute>() != null);

            foreach (var prop in properties)
            {
                var currentValue = prop.GetValue(this);
                if (!OriginalValues.ContainsKey(prop.Name) ||
                    !Equals(OriginalValues[prop.Name], currentValue))
                {
                    // Console.WriteLine($"HasChanges: {prop.Name} is modified");

                    IsModified = true;
                    return true;
                }
            }

            // Check relationship keys
            foreach (var prop in relationshipProps)
            {
                var currentValue = getSnapshotRelationValue(prop);
                if (!RelationshipKeys.ContainsKey(prop.Name) ||
                    !Equals(RelationshipKeys[prop.Name], currentValue))
                {
                    IsModified = true;
                    return true;
                }
            }
            //console.WriteLine("No changes");
            return false;
        }
    }
}
