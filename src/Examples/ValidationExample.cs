using MyORM.Attributes;
using MyORM.Attributes.Validation;
using MyORM.Core;
// using System.Collections.Generic;
// using System.ComponentModel.DataAnnotations;

namespace MyORM.Examples
{
    [Table("Users")]
    public class User : Entity
    {
        [Key]
        [Column("Id", false)]
        public int Id { get; set; }

        [Required]
        [StringLength(2, 20)]
        [Column("Username", false)]
        public string Username { get; set; }

        [Required]
        [Email]
        [Column("Email", false)]
        public string Email { get; set; }

        [Required]
        [StringLength(20, 50)]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$")] // At least 8 characters, 1 letter and 1 number
        [Column("Password", false)]
        public string Password { get; set; }

        [Range(13, 120)]
        [Column("Age", false)]
        public int Age { get; set; }
    }

    public class ValidationExample
    {
        public void RunExample()
        {
            var user = new User
            {
                Username = "j", // Too short
                Email = "invalid-email", // Invalid email format
                Password = "weak", // Too short and doesn't meet regex
                Age = 10 // Below minimum
            };

            var validationResults = ValidateEntity(user);
            PrintValidationResults(validationResults);

            // Fix the validation errors
            user.Username = "john_doe";
            user.Email = "john@example.com";
            user.Password = "SecurePass123";
            user.Age = 25;

            validationResults = ValidateEntity(user);
            PrintValidationResults(validationResults);
        }

        private List<ValidationResult> ValidateEntity<T>(T entity) where T : Entity
        {
            var results = new List<ValidationResult>();
            var properties = typeof(T).GetProperties();

            foreach (var property in properties)
            {
                var value = property.GetValue(entity);
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

            return results;
        }

        private void PrintValidationResults(List<ValidationResult> results)
        {
            if (results.Count == 0)
            {
                Console.WriteLine("✅ Validation passed! No errors found.");
                return;
            }

            Console.WriteLine("❌ Validation failed! Found the following errors:");
            foreach (var result in results)
            {
                var errorIcon = result.ErrorLevel switch
                {
                    ValidationErrorLevel.Warning => "⚠️",
                    ValidationErrorLevel.Error => "❌",
                    ValidationErrorLevel.Critical => "🚫",
                    _ => "❓"
                };

                Console.WriteLine($"{errorIcon} {result.PropertyName}: {result.Message} ({result.ErrorLevel})");
            }
            Console.WriteLine();
        }
    }
} 