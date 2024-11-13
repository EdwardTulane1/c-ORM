using MyORM.Attributes;
using MyORM.Attributes.Validation;
using MyORM.Core;

namespace MyORM.Examples
{
    [Table("Users")]
    public class User : Entity
    {
        [Key]
        [Column("Id", false)]
        public int Id { get; set; }

        [Required]
        [StringLength(20, 2)]
        [Column("Username", false)]
        public virtual string Username { get; set; }

        [Required]
        [Email]
        [Column("Email", false)]
        public virtual string Email { get; set; }

        [Required]
        [StringLength(50, 1)]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$")] // At least 8 characters, 1 letter and 1 number
        [Column("Password", false)]
        public virtual string Password { get; set; }

        [Range(2, 120)]
        [Column("Age", false)]
        public virtual int Age { get; set; }
    }

    public class myExm : DbContext
    {
        public static readonly string XmlStoragePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "XmlStorage"
        );

        public DbSet<User> Users { get; set; }

        public myExm() : base(XmlStoragePath)
        {
            Console.WriteLine($"XML files will be stored in: {XmlStoragePath}");
        }
    }


    public class ValidationExample
    {
        public void RunExample()
        {
            Console.WriteLine("Running Validation Example...\n");

            // Create context
            var context = new myExm();


            // Create a new user
            var user = new User();


            // Create a new user through the DbSet to get a proxied instance

            Console.WriteLine("Setting invalid values:");
            // These should trigger validation errors
            user.Username = "jhon"; // Too short
            user.Email = "invalidemail@zzz"; // Invalid email format
            user.Password = "Password123"; // Too short and doesn't meet regex
            user.Age = 10; // Below minimum

            Console.WriteLine("\nSetting valid values:");
            // Now set valid values
            // user.Username = "john_doe";
            // user.Email = "john@example.com";
            // user.Password = "SecurePass123";
            // user.Age = 25;

            // Add to context
            context.Users.Add(user);
            context.SaveChanges();
        }
    }
}