using System;

namespace MyORM.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RelationshipAttribute : Attribute
    {
        public RelationType Type { get; }
        public Type RelatedType { get; }
        public string FromProperty { get; }
        public string ToProperty { get; }

        public RelationshipAttribute(
            RelationType type, 
            Type relatedType, 
            string fromProperty,
            string toProperty = "Id")
        {
            Type = type;
            RelatedType = relatedType;
            FromProperty = fromProperty;
            ToProperty = toProperty;
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

