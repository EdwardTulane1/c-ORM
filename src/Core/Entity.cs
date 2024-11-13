using System;

namespace MyORM.Core
{
    public abstract class Entity
    {
        public virtual bool IsNew { get; internal set; } = true; // on 1st save will be set to false
        public virtual bool IsModified { get; internal set; }
        public virtual bool IsDeleted { get; internal set; }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (!IsNew)
            {
                IsModified = true;
            }
        }
    }
}
