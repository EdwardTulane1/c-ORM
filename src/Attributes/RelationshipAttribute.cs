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
        public DeleteBehavior OnDelete { get; set; } = DeleteBehavior.Cascade;

        public RelationshipAttribute(
            RelationType type, 
            Type relatedType, 
            string fromProperty,
            string toProperty = "Id",
            DeleteBehavior onDelete = DeleteBehavior.Cascade)
        {
            Type = type;
            RelatedType = relatedType;
            FromProperty = fromProperty;
            ToProperty = toProperty;
            OnDelete = onDelete;
        }
    }

    public enum RelationType
    {
        OneToOne,
        OneToMany,
        ManyToOne,
        ManyToMany
    }

    public enum DeleteBehavior
    {
        Cascade,
        SetNull,
        Restrict
    }
}

