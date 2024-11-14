using System;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MyORM.Attributes.Validation;

namespace MyORM.Core
{
    public abstract class Entity : INotifyPropertyChanged
    {
        public virtual bool IsNew { get; internal set; } = true;
        public virtual bool IsModified { get; internal set; }
        public virtual bool IsDeleted { get; internal set; }

        public event PropertyChangedEventHandler PropertyChanged;  

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")  
        {  
            OnPropertyChanged(propertyName);
        }  

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
