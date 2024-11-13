using System;
using System.Reflection;
using MyORM.Attributes.Validation;

namespace MyORM.Core
{
    public abstract class Entity
    {
        public virtual bool IsNew { get; internal set; } = true;
        public virtual bool IsModified { get; internal set; }
        public virtual bool IsDeleted { get; internal set; }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (!IsNew)
            {
                IsModified = true;
            }
            Console.WriteLine($"Property {propertyName} has been changed");
            ValidationHelper.ValidateEntity(this);
        }
    }

}
