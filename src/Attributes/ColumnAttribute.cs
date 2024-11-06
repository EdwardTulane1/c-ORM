using System;

namespace MyORM.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {
        public string ColumnName { get; }
        public bool IsNullable { get; }

        public ColumnAttribute(string columnName, bool isNullable = true)
        {
            ColumnName = columnName;
            IsNullable = isNullable;
        }
    }
}
