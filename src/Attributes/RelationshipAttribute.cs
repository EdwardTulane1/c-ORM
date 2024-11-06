using System;

namespace MyORM.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RelationshipAttribute : Attribute
    {
        public RelationType Type { get; }
        public Type ForeignType { get; }

        public RelationshipAttribute(RelationType type, Type foreignType)
        {
            Type = type;
            ForeignType = foreignType;
        }
    }

    public enum RelationType
    {
        OneToOne,
        OneToMany,
        ManyToOne,
        ManyToMany
    }
}

