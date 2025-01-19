using MyORM.Attributes;
using MyORM.Core;
using MyORM.Attributes.Validation;
using System;
using System.Linq;

namespace MyORM.Tests
{
    [Table("ValidatedEntities")]
    public class ValidatedEntity : Entity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [StringLength(50, 2)]
        [Column("Name")]
        public string Name { get; set; }

        [Email]
        [Column("Email")]
        public string Email { get; set; }

        [Range(0, 100)]
        [Column("Score")]
        public int Score { get; set; }

        [RegularExpression(@"^[A-Z]{2}\d{3}$")]
        [Column("Code")]
        public string Code { get; set; }
    }

    public class ValidationContext : DbContext
    {
        public static readonly string XmlStoragePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "XmlStorage"
        );

        public DbSet<ValidatedEntity> ValidatedEntities { get; set; }

        public ValidationContext() : base(XmlStoragePath) { }
    }

    public class Test5_Validation
    {
        private readonly ValidationContext _context;

        public Test5_Validation()
        {
            _context = new ValidationContext();
        }

        public void RunAllTests()
        {
            Console.WriteLine("Running Validation Tests...");

            TestRequiredValidation();
            TestStringLengthValidation();
            TestEmailValidation();
            TestRangeValidation();
            TestRegexValidation();
            TestMultipleValidations();

            _context.Dispose();

            Console.WriteLine("Validation Tests completed.");
        }

        private void TestRequiredValidation()
        {
            Console.WriteLine("\nTesting Required Validation...");

            var entity = new ValidatedEntity { Id = 1, Score = 50 };
            _context.ValidatedEntities.Add(entity);

            try
            {
                _context.SaveChanges();
                Console.WriteLine("Error: Required validation failed to catch null Name");
            }
            catch (ValidationException ex)
            {
                Console.WriteLine($"1. Validation caught successfully: {ex.Message}");
                _context.ValidatedEntities.Remove(entity);
            }
        }

        private void TestStringLengthValidation()
        {
            Console.WriteLine("\nTesting String Length Validation...");
            var entity = new ValidatedEntity
            {
                Id = 2,
                Name = "A", // Too short
                Score = 50
            };
            _context.ValidatedEntities.Add(entity);
            try
            {

                _context.SaveChanges();
                Console.WriteLine("Error: String length validation failed");
            }
            catch (ValidationException ex)
            {
                Console.WriteLine($"2. Validation caught successfully: {ex.Message}");
                _context.ValidatedEntities.Remove(entity);
            }
        }

        private void TestEmailValidation()
        {
            Console.WriteLine("\nTesting Email Validation...");

            var entity = new ValidatedEntity
            {
                Id = 3,
                Name = "Test Entity",
                Email = "invalid-email",
                Score = 50
            };
            _context.ValidatedEntities.Add(entity);
            try
            {

                _context.SaveChanges();
                Console.WriteLine("Error: Email validation failed");
            }
            catch (ValidationException ex)
            {
                Console.WriteLine($"3. Validation caught successfully: {ex.Message}");
                _context.ValidatedEntities.Remove(entity);
            }
        }

        private void TestRangeValidation()
        {
            Console.WriteLine("\nTesting Range Validation...");
            var entity = new ValidatedEntity
            {
                Id = 4,
                Name = "Test Entity",
                Score = 101 // Outside valid range
            };
            _context.ValidatedEntities.Add(entity);
            try
            {

                _context.SaveChanges();
                Console.WriteLine("Error: Range validation failed");
            }
            catch (ValidationException ex)
            {
                Console.WriteLine($"4. Validation caught successfully: {ex.Message}");
                _context.ValidatedEntities.Remove(entity);
            }
        }

        private void TestRegexValidation()
        {
            Console.WriteLine("\nTesting Regex Validation...");
            var entity = new ValidatedEntity
            {
                Id = 5,
                Name = "Test Entity",
                Code = "invalid-code" // Should be format XX999
            };
            _context.ValidatedEntities.Add(entity);
            try
            {

                _context.SaveChanges();
                Console.WriteLine("Error: Regex validation failed");
            }
            catch (ValidationException ex)
            {
                Console.WriteLine($"5. Validation caught successfully: {ex.Message}");
                _context.ValidatedEntities.Remove(entity);
            }
        }

        private void TestMultipleValidations()
        {
            Console.WriteLine("\nTesting Multiple Validations...");

            // Test valid entity
            var validEntity = new ValidatedEntity
            {
                Id = 6,
                Name = "Valid Test Entity",
                Email = "test@example.com",
                Score = 75,
                Code = "AB123"
            };
            _context.ValidatedEntities.Add(validEntity);
            try
            {

                _context.SaveChanges();
                Console.WriteLine("Valid entity saved successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"6. Error: Valid entity failed validation: {ex.Message}");
                _context.ValidatedEntities.Remove(validEntity);
            }
        }
    }
}