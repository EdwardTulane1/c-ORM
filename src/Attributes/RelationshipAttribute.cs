using System;

namespace MyORM.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RelationshipAttribute : Attribute
    {
        public RelationType Type { get; }
        public Type RelatedType { get; }
        public DeleteBehavior OnDelete { get; set; } = DeleteBehavior.Cascade;

        public RelationshipAttribute(
            RelationType type, 
            Type relatedType,
            DeleteBehavior onDelete = DeleteBehavior.None)
        {
            Type = type;
            RelatedType = relatedType;
            switch (type)
            {
                case RelationType.ManyToMany:
                    if(onDelete != DeleteBehavior.None && onDelete != DeleteBehavior.Cascade)
                    {
                        throw new Exception("ManyToMany relationship must have a delete behavior of Cascade or None");
                    }
                    break;
                case RelationType.OneToMany:
                    if(onDelete != DeleteBehavior.Cascade && onDelete != DeleteBehavior.SetNull)
                    {
                        throw new Exception("OneToMany relationship must have a delete behavior of Cascade or SetNull");
                    }
                    break;
                case RelationType.ManyToOne:
                    if(onDelete != DeleteBehavior.None)
                    {
                        throw new Exception("ManyToOne relationship must have a delete behavior of None");
                    }
                    break;
                case RelationType.OneToOne:
                    if(onDelete != DeleteBehavior.Cascade && onDelete != DeleteBehavior.SetNull && onDelete != DeleteBehavior.Orphan)
                    {
                        throw new Exception("OneToOne relationship must have a delete behavior of Cascade, SetNull, or Orphan");
                    }
                    break;
                
            }
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
        Orphan,

        None
    }
}


