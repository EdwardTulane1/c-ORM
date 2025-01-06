using System;
using System.Linq;

namespace MyORM.Tests
{
    public class TestSetNullBehavior
    {
        private readonly SetNullContext _context;

        public TestSetNullBehavior()
        {
            _context = new SetNullContext();
        }

        public void RunAllTests()
        {
            Console.WriteLine("Running SetNull Behavior Tests...");
            
            TestSetNullOnDelete();
            
            Console.WriteLine("SetNull Behavior Tests completed.");
        }

        private void TestSetNullOnDelete()
        {
            Console.WriteLine("\nTesting SetNull on Delete...");

            // Create test data
            var author = new Author { Id = 1, Name = "Test Author" };
            var book1 = new Book { Id = 1, Title = "Book 1", Author = author };
            var book2 = new Book { Id = 2, Title = "Book 2", Author = author };

            _context.Authors.Add(author);
            _context.Books.Add(book1);
            _context.Books.Add(book2);
            _context.SaveChanges();

            var book =  _context.Query<Book>().Where("Id", "=", "1").Execute().FirstOrDefault();
            Console.WriteLine($"Book: {book.Title}, Author: {(book.Author == null ? "Null" : book.Author.Name)}");

            // Delete the author
            _context.Authors.Remove(author);
            _context.SaveChanges();

            // Verify that the books' author references are set to null
            var books = _context.Query<Book>().Execute();
            foreach (var b in books)
            {
                Console.WriteLine($"Book: {b.Title}, Author: {(b.Author == null ? "Null" : b.Author.Name)}");
            }
        }
    }
} 