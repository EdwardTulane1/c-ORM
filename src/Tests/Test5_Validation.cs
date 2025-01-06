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
        [Key(isAutoIncrement: false)]
        [Column("Id", false)]
        public int Id { get; set; }

        [Required]
        [StringLength(50, 2)]
        [Column("Name", false)]
        public string Name { get; set; }

        [Email]
        [Column("Email", false)]
        public string Email { get; set; }

        [Range(0, 100)]
        [Column("Score", false)]
        public int Score { get; set; }

        [RegularExpression(@"^[A-Z]{2}\d{3}$")]
        [Column("Code", false)]
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
            
            Console.WriteLine("Validation Tests completed.");
        }

        private void TestRequiredValidation()
        {
            Console.WriteLine("\nTesting Required Validation...");
            
            try
            {
                var entity = new ValidatedEntity { Id = 1, Score = 50 };
                _context.ValidatedEntities.Add(entity);
                _context.SaveChanges();
                Console.WriteLine("Error: Required validation failed to catch null Name");
            }
            catch (ValidationException ex)
            {
                Console.WriteLine($"Validation caught successfully: {ex.Message}");
            }
        }

        private void TestStringLengthValidation()
        {
            Console.WriteLine("\nTesting String Length Validation...");
            
            try
            {
                var entity = new ValidatedEntity 
                { 
                    Id = 2, 
                    Name = "A", // Too short
                    Score = 50 
                };
                _context.ValidatedEntities.Add(entity);
                _context.SaveChanges();
                Console.WriteLine("Error: String length validation failed");
            }
            catch (ValidationException ex)
            {
                Console.WriteLine($"Validation caught successfully: {ex.Message}");
            }
        }

        private void TestEmailValidation()
        {
            Console.WriteLine("\nTesting Email Validation...");
            
            try
            {
                var entity = new ValidatedEntity 
                { 
                    Id = 3,
                    Name = "Test Entity",
                    Email = "invalid-email",
                    Score = 50
                };
                _context.ValidatedEntities.Add(entity);
                _context.SaveChanges();
                Console.WriteLine("Error: Email validation failed");
            }
            catch (ValidationException ex)
            {
                Console.WriteLine($"Validation caught successfully: {ex.Message}");
            }
        }

        private void TestRangeValidation()
        {
            Console.WriteLine("\nTesting Range Validation...");
            
            try
            {
                var entity = new ValidatedEntity 
                { 
                    Id = 4,
                    Name = "Test Entity",
                    Score = 101 // Outside valid range
                };
                _context.ValidatedEntities.Add(entity);
                _context.SaveChanges();
                Console.WriteLine("Error: Range validation failed");
            }
            catch (ValidationException ex)
            {
                Console.WriteLine($"Validation caught successfully: {ex.Message}");
            }
        }

        private void TestRegexValidation()
        {
            Console.WriteLine("\nTesting Regex Validation...");
            
            try
            {
                var entity = new ValidatedEntity 
                { 
                    Id = 5,
                    Name = "Test Entity",
                    Code = "invalid-code" // Should be format XX999
                };
                _context.ValidatedEntities.Add(entity);
                _context.SaveChanges();
                Console.WriteLine("Error: Regex validation failed");
            }
            catch (ValidationException ex)
            {
                Console.WriteLine($"Validation caught successfully: {ex.Message}");
            }
        }

        private void TestMultipleValidations()
        {
            Console.WriteLine("\nTesting Multiple Validations...");
            
            // Test valid entity
            try
            {
                var validEntity = new ValidatedEntity 
                { 
                    Id = 6,
                    Name = "Valid Test Entity",
                    Email = "test@example.com",
                    Score = 75,
                    Code = "AB123"
                };
                _context.ValidatedEntities.Add(validEntity);
                _context.SaveChanges();
                Console.WriteLine("Valid entity saved successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Valid entity failed validation: {ex.Message}");
            }
        }
    }
} 