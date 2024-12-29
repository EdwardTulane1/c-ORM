using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using MyORM.Core;
using MyORM.Helper;


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
        public string? ErrorMessage { get; set; }
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
                return new ValidationResult(false, ErrorMessage!, propertyName, ErrorLevel);
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
                return new ValidationResult(false, ErrorMessage!, propertyName, ErrorLevel);
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
                return new ValidationResult(false, ErrorMessage!, propertyName, ErrorLevel);
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
                return new ValidationResult(false, ErrorMessage!, propertyName, ErrorLevel);
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
                return new ValidationResult(false, ErrorMessage!, propertyName, ErrorLevel);
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

    public class ValidationException : Exception
    {
        public List<ValidationResult> ValidationResults { get; }

        public ValidationException(List<ValidationResult> results)
            : base(FormatValidationMessage(results))
        {
            ValidationResults = results;
        }

        private static string FormatValidationMessage(List<ValidationResult> results)
        {
            var errorMessages = results
                .Select(r => $"{r.PropertyName}: {r.Message} ({r.ErrorLevel})")
                .ToList();

            return $"Validation failed with {results.Count} errors:\n" +
                   string.Join("\n", errorMessages);
        }
    }

    public static class ValidationHelper
    {
        private static ValidationResult ValidateKeyProperty(object entity)
        {
            var keyProps = entity.GetType().GetProperties()
                .Where(p => p.GetCustomAttribute<KeyAttribute>() != null)
                .ToList();

            if (keyProps.Count == 0)
            {
                return new ValidationResult(
                    false, 
                    $"Entity {entity.GetType().Name} must have at least one key property", 
                    "Key", 
                    ValidationErrorLevel.Critical);
            }

            if (keyProps.Count > 1)
            {
                return new ValidationResult(
                    false, 
                    $"Entity {entity.GetType().Name} cannot have multiple key properties", 
                    "Key", 
                    ValidationErrorLevel.Critical);
            }

            return ValidationResult.Success;
        }

        private static ValidationResult ValidateKeyUniqueness(object entity)
        {
            var keyProp = HelperFuncs.GetKeyProperty(entity.GetType());
            var keyValue = keyProp.GetValue(entity)?.ToString();
            var entityType = entity.GetType();
            // Console.WriteLine($"entityType: {entityType.Name}, keyValue: {keyValue}. isNew: {((Entity)entity).IsNew}");

            // Check if entity is new and key already exists
            // BIG TODO - the IsNew is always true. Has to check
            // Console.WriteLine($"Validating key uniqueness for entity: {entity.GetType().Name}, key: {keyValue}, isNew: {((Entity)entity).IsNew}");
            if (((Entity)entity).IsNew)
            {
                var xmlPath = HelperFuncs.GetTablePath(
                    Path.Combine(Directory.GetCurrentDirectory(), "XmlStorage"), 
                    entityType.Name);

                if (File.Exists(xmlPath))
                {
                    var doc = XDocument.Load(xmlPath);
                    var existingEntity = doc.Root?.Elements("Entity")
                        .FirstOrDefault(e => e.Element(keyProp.Name)?.Value == keyValue);

                    if (existingEntity != null)
                    {
                        return new ValidationResult(
                            false,
                            $"An entity of type {entityType.Name} with key {keyValue} already exists",
                            keyProp.Name,
                            ValidationErrorLevel.Critical);
                    }
                }
            }

            return ValidationResult.Success;
        }

        public static void ValidateEntity(object entity)
        {
            // Console.WriteLine($"Validating entity of type {entity.GetType().Name}");
            
            var results = new List<ValidationResult>();

            // Global validations
            results.Add(ValidateKeyProperty(entity));
            results.Add(ValidateKeyUniqueness(entity));

            // Property-level validations
            var properties = entity.GetType().GetProperties();
            foreach (var property in properties)
            {
                var value = property.GetValue(entity)!;
                var validationAttributes = property.GetCustomAttributes(typeof(ValidationAttribute), true) as ValidationAttribute[];

                if (validationAttributes != null)
                {
                    foreach (var attribute in validationAttributes)
                    {
                        var result = attribute.Validate(value, property.Name);
                        if (!result.IsValid)
                        {
                            results.Add(result);
                        }
                    }
                }
            }

            // If there are any errors (not warnings), throw exception
            var errors = results.Where(r => r.ErrorLevel >= ValidationErrorLevel.Error && !r.IsValid).ToList();
            if (errors.Any())
            {
                LogValidationErrors(errors);
                throw new ValidationException(errors);
            }
        }

        private static void LogValidationErrors(List<ValidationResult> errors)
        {
            Console.WriteLine("❌ Validation failed! Found the following errors:");
            foreach (var error in errors)
            {
                var errorIcon = error.ErrorLevel switch
                {
                    ValidationErrorLevel.Warning => "⚠️",
                    ValidationErrorLevel.Error => "❌",
                    ValidationErrorLevel.Critical => "🚫",
                    _ => "❓"
                };

                Console.WriteLine($"{errorIcon} {error.PropertyName}: {error.Message} ({error.ErrorLevel})");
            }
            Console.WriteLine();
        }
    }
}



// Otherwise validate all properties