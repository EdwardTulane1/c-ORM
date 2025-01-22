using MyORM.Attributes;
using MyORM.Core;
using MyORM.Attributes.Validation;
namespace MyORM.UsageExample
{
    // Entity definitions
    [Table("Books")]
    public class Book : Entity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [StringLength(100, 2)]
        [Column("Title")]
        public string Title { get; set; }

        [Column("Price")]
        [Range(0, 1000)]
        public float Price { get; set; }

        [Relationship(RelationType.ManyToOne, typeof(Author), onDelete: DeleteBehavior.None)]
        public virtual Author Author { get; set; }

        [Relationship(RelationType.ManyToMany, typeof(Category), onDelete: DeleteBehavior.None)]
        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
    }

    [Table("Authors")]
    public class Author : Entity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [StringLength(100, 3)]
        [Column("Name")]
        public string Name { get; set; }

        [Email]
        [Column("Email")]
        public string Email { get; set; }

        [Relationship(RelationType.OneToMany, typeof(Book), onDelete: DeleteBehavior.Cascade)]
        public virtual ICollection<Book> Books { get; set; } = new List<Book>();

        [Relationship(RelationType.OneToOne, typeof(AuthorProfile), onDelete: DeleteBehavior.Orphan)]
        public virtual AuthorProfile Profile { get; set; }
    }

    [Table("Categories")]
    public class Category : Entity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [Column("Name")]
        public string Name { get; set; }

        [Relationship(RelationType.ManyToMany, typeof(Book), onDelete: DeleteBehavior.None)]
        public virtual ICollection<Book> Books { get; set; } = new List<Book>();
    }

    [Table("AuthorProfiles")]
    public class AuthorProfile : Entity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Bio")]
        public string Bio { get; set; }

        [Relationship(RelationType.OneToOne, typeof(Author), onDelete: DeleteBehavior.SetNull)]
        public virtual Author Author { get; set; }
    }

    // DbContext definition
    public class BookstoreContext : DbContext
    {
        public DbSet<Book> Books { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<AuthorProfile> AuthorProfiles { get; set; }

        public BookstoreContext() : base() { }
    }

    public class BookstoreExample
    {
        private readonly BookstoreContext _context;

        public BookstoreExample()
        {
            _context = new BookstoreContext();
        }

        public void RunExample()
        {
            Console.WriteLine("MyORM Usage Example - Bookstore\n");

            // Basic CRUD Operations
            // BasicCrudExample();

            // // Relationships
            // RelationshipsExample();

            // // Querying
            // QueryingExample();

            // // Validation
            // ValidationExample();

            // Delete behaviors
            DeleteExample();

            _context.Dispose();
        }

        private void BasicCrudExample()
        {
            Console.WriteLine("1. Basic CRUD Operations");

            // Create
            var author = new Author 
            { 
                Id = 1, 
                Name = "John Doe",
                Email = "john@example.com"
            };
            _context.Authors.Add(author);
            _context.SaveChanges();

            // Read
            var savedAuthor = _context.Query<Author>()
                .Where("Id", "=", "1")
                .Execute()
                .FirstOrDefault();
            Console.WriteLine($"Created author: {savedAuthor?.Name}");

            // Update
            if (savedAuthor != null)
            {
                savedAuthor.Name = "John Smith";
                _context.SaveChanges();
                Console.WriteLine($"Updated author name: {savedAuthor.Name}");
            }

            // // Delete
            // _context.Authors.Remove(savedAuthor);
            // _context.SaveChanges();
            // Console.WriteLine("Author deleted");
            
        }

        private void RelationshipsExample()
        {
            Console.WriteLine("\n2. Working with Relationships");

            // One-to-Many
            var author = new Author { Id = 2, Name = "Jane Doe", Email = "jane@example.com" };
            var book1 = new Book { Id = 1, Title = "First Book", Price = 29.99f, Author = author };
            var book2 = new Book { Id = 2, Title = "Second Book", Price = 39.99f, Author = author };

            _context.Authors.Add(author);
            _context.Books.Add(book1);
            _context.Books.Add(book2);

            // Many-to-Many
            var fictionCategory = new Category { Id = 1, Name = "Fiction" };
            var mysteryCategory = new Category { Id = 2, Name = "Mystery" };

            _context.Categories.Add(fictionCategory);
            _context.Categories.Add(mysteryCategory);

            book1.Categories.Add(fictionCategory);
            book1.Categories.Add(mysteryCategory);
            book2.Categories.Add(fictionCategory);

            // One-to-One
            var profile = new AuthorProfile 
            { 
                Id = 1,
                Bio = "Bestselling author of mystery novels",
                Author = author 
            };
            author.Profile = profile;
            _context.AuthorProfiles.Add(profile);

            _context.SaveChanges();
            Console.WriteLine("Relationships created successfully");
        }

        private void QueryingExample()
        {
            Console.WriteLine("\n3. Querying Data");

            // Basic filtering
            var expensiveBooks = _context.Query<Book>()
                .Where("Price", ">", "30.00")
                .Execute();
            Console.WriteLine($"Expensive books count: {expensiveBooks.Count()}");

            // Multiple conditions
            var books = _context.Query<Book>()
                .Where("Price", ">", "20.00")
                .Where("Price", "<", "40.00")
                .Execute();

            foreach (var book in books)
            {
                Console.WriteLine($"Book: {book.Title}, Price: ${book.Price}");
                Console.WriteLine($"Author: {book.Author?.Name}.");
                Console.WriteLine($"Categories: {string.Join(", ", book.Categories.Select(c => c.Name))}");
            }
        }

        private void ValidationExample()
        {
            Console.WriteLine("\n4. Validation Examples");

            try
            {
                var invalidAuthor = new Author
                {
                    Id = 99,
                    Name = "T",
                    Email = "invalid-email" // Invalid email format
                };
                _context.Authors.Add(invalidAuthor);
                _context.SaveChanges();
            }
            catch (ValidationException ex)
            {
                Console.WriteLine($"Email validation failed as expected: {ex.Message}");
            }
        }

        private void DeleteExample()
        {
            Console.WriteLine("\n5. Testing Delete Behaviors");

            // Setup test data
            var author = new Author { Id = 3, Name = "Delete Test Author", Email = "delete@test.com" };
            var book1 = new Book { Id = 10, Title = "Delete Test Book 1", Price = 19.99f, Author = author };
            var book2 = new Book { Id = 11, Title = "Delete Test Book 2", Price = 29.99f, Author = author };
            var profile = new AuthorProfile { Id = 2, Bio = "Test Bio", Author = author };
            var category = new Category { Id = 3, Name = "Test Category" };

            // Setup relationships
            author.Books.Add(book1);
            author.Books.Add(book2);
            author.Profile = profile;
            book1.Categories.Add(category);
            book2.Categories.Add(category);

            // Save all entities
            _context.Authors.Add(author);
            _context.Books.Add(book1);
            _context.Books.Add(book2);
            _context.Categories.Add(category);
            _context.AuthorProfiles.Add(profile);
            _context.SaveChanges();

            Console.WriteLine("Initial state:");
            Console.WriteLine($"Author: {author.Name}");
            Console.WriteLine($"Books count: {author.Books.Count}");
            Console.WriteLine($"Profile exists: {author.Profile != null}");

            // Delete author (will trigger cascade delete for books and orphan delete for profile)
            Console.WriteLine("\nDeleting author...");
            // return;
            _context.Authors.Remove(author);
            _context.SaveChanges();

            // Verify deletions
            var deletedAuthor = _context.Query<Author>()
                .Where("Id", "=", "3")
                .Execute()
                .FirstOrDefault();
            var deletedBooks = _context.Query<Book>()
                .Where("Author", "=", "3")
                .Execute();
            var deletedProfile = _context.Query<AuthorProfile>()
                .Where("Id", "=", "2")
                .Execute()
                .FirstOrDefault();
            var categoryAfterDelete = _context.Query<Category>()
                .Where("Id", "=", "3")
                .Execute()
                .FirstOrDefault();

            Console.WriteLine("\nAfter delete:");
            Console.WriteLine($"Author exists: {deletedAuthor != null}");
            Console.WriteLine($"Books exist: {deletedBooks.Any()}");
            Console.WriteLine($"Profile exists: {deletedProfile != null}");
            Console.WriteLine($"Category still exists: {categoryAfterDelete != null}");
        }
    }
}
