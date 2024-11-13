using System;

namespace MyORM.Attributes.Validation
{
    public enum ValidationErrorLevel
    {
        Warning,
        Error,
        Critical
    }

    public enum ValidationRuleType
    {
        Required,
        StringLength,
        Range,
        Email,
        RegularExpression,
        Custom
    }

    public class ValidationResult
    {
        public bool IsValid { get; }
        public string Message { get; }
        public string PropertyName { get; }
        public ValidationErrorLevel ErrorLevel { get; }

        public static ValidationResult Success => new ValidationResult(true, string.Empty, string.Empty, ValidationErrorLevel.Warning);

        public ValidationResult(bool isValid, string message, string propertyName, ValidationErrorLevel errorLevel)
        {
            IsValid = isValid;
            Message = message;
            PropertyName = propertyName;
            ErrorLevel = errorLevel;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public abstract class ValidationAttribute : Attribute
    {
        public string ErrorMessage { get; set; }
        public ValidationErrorLevel ErrorLevel { get; set; } = ValidationErrorLevel.Error;
        public ValidationRuleType RuleType { get; protected set; }
        
        public abstract ValidationResult Validate(object value, string propertyName);
    }

    public class RequiredAttribute : ValidationAttribute
    {
        public RequiredAttribute()
        {
            RuleType = ValidationRuleType.Required;
            ErrorMessage = "Field is required";
        }

        public override ValidationResult Validate(object value, string propertyName)
        {
            if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
            {
                return new ValidationResult(false, ErrorMessage, propertyName, ErrorLevel);
            }
            return ValidationResult.Success;
        }
    }

    public class StringLengthAttribute : ValidationAttribute
    {
        public int MinLength { get; }
        public int MaxLength { get; }

        public StringLengthAttribute(int maxLength, int minLength = 0)
        {
            MaxLength = maxLength;
            MinLength = minLength;
            RuleType = ValidationRuleType.StringLength;
            ErrorMessage = $"String length must be between {minLength} and {maxLength}";
        }

        public override ValidationResult Validate(object value, string propertyName)
        {
            if (value == null) return ValidationResult.Success;
            
            var str = value as string;
            if (str != null && (str.Length < MinLength || str.Length > MaxLength))
            {
                return new ValidationResult(false, ErrorMessage, propertyName, ErrorLevel);
            }
            return ValidationResult.Success;
        }
    }

    public class RangeAttribute : ValidationAttribute
    {
        public int MinValue { get; }
        public int MaxValue { get; }

        public RangeAttribute(int minValue, int maxValue)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            RuleType = ValidationRuleType.Range;
            ErrorMessage = $"Value must be between {minValue} and {maxValue}";
        }

        public override ValidationResult Validate(object value, string propertyName)
        {
            if (value == null) return ValidationResult.Success;
            
            var number = value as int?;
            if (number != null && (number < MinValue || number > MaxValue))
            {
                return new ValidationResult(false, ErrorMessage, propertyName, ErrorLevel);
            }
            return ValidationResult.Success;
        }
    }

    public class EmailAttribute : ValidationAttribute
    {
        public EmailAttribute()
        {
            RuleType = ValidationRuleType.Email;
            ErrorMessage = "Invalid email format";
        }

        public override ValidationResult Validate(object value, string propertyName)
        {
            if (value == null) return ValidationResult.Success;
            
            var str = value as string;
            if (str != null && !str.Contains("@"))
            {
                return new ValidationResult(false, ErrorMessage, propertyName, ErrorLevel);
            }
            return ValidationResult.Success;
        }
    }

    public class RegularExpressionAttribute : ValidationAttribute
    {
        public string Pattern { get; }

        public RegularExpressionAttribute(string pattern)
        {
            Pattern = pattern;
            RuleType = ValidationRuleType.RegularExpression;
            ErrorMessage = "Invalid format";
        }

        public override ValidationResult Validate(object value, string propertyName)
        {
            if (value == null) return ValidationResult.Success;
            
            var str = value as string;
            if (str != null && !System.Text.RegularExpressions.Regex.IsMatch(str, Pattern))
            {
                return new ValidationResult(false, ErrorMessage, propertyName, ErrorLevel);
            }
            return ValidationResult.Success;
        }
    }

    public class CustomValidationAttribute : ValidationAttribute
    {
        public Func<object, ValidationResult> ValidationFunction { get; }

        public CustomValidationAttribute(Func<object, ValidationResult> validationFunction)
        {
            ValidationFunction = validationFunction;
            RuleType = ValidationRuleType.Custom;
        }

        public override ValidationResult Validate(object value, string propertyName)
        {
            return ValidationFunction(value);
        }
    }
} 


