using MyORM.Attributes;
using MyORM.Core;
using System;
using System.Linq;

namespace MyORM.Tests
{
    public class Test2_Relationships
    {
        private readonly Test1 _context;

        public Test2_Relationships()
        {
            _context = new Test1();
        }

        public void RunAllTests()
        {
            Console.WriteLine("Running Relationship Tests...");
            
            TestOneToManyRelationship();
            TestCascadeDelete();
            TestSetNullBehavior();
            TestRelationshipLoading();
            
            Console.WriteLine("Relationship Tests completed.");
        }

        private void TestOneToManyRelationship()
        {
            Console.WriteLine("\nTesting One-to-Many Relationship...");
            
            var manufacturer = new Manufacturer { Id = 301, Name = "BMW Test" };
            var car1 = new Car { Id = 301, Name = "Test Car 1", Price = 30000, Manufacturer = manufacturer };
            var car2 = new Car { Id = 302, Name = "Test Car 2", Price = 40000, Manufacturer = manufacturer };

            _context.Manufacturers.Add(manufacturer);
            _context.Cars.Add(car1);
            _context.Cars.Add(car2);
            _context.SaveChanges();

            // Test loading relationship
            var query = _context.Query<Manufacturer>();
            var loaded = query.Where("Id", "=", "301").Execute().FirstOrDefault();
            Console.WriteLine($"Number of cars for manufacturer: {loaded?.Cars.Count ?? 0}");
        }

        private void TestCascadeDelete()
        {
            Console.WriteLine("\nTesting Cascade Delete...");

            var manufacturer = new Manufacturer { Id = 401, Name = "Cascade Test" };
            var car = new Car { Id = 401, Name = "Cascade Car", Price = 50000, Manufacturer = manufacturer };
            
            _context.Manufacturers.Add(manufacturer);
            _context.Cars.Add(car);
            _context.SaveChanges();

            _context.Manufacturers.Remove(manufacturer);
            _context.SaveChanges();

            // Verify both manufacturer and car are deleted
            var carQuery = _context.Query<Car>();
            var deletedCar = carQuery.Where("Id", "=", "401").Execute().FirstOrDefault();
            Console.WriteLine($"Cascade deleted car found: {(deletedCar == null ? "No" : "Yes")}");
        }

        private void TestSetNullBehavior()
        {
            Console.WriteLine("\nTesting SetNull Behavior...");
            
            // Create manufacturer with SetNull delete behavior
            var manufacturer = new Manufacturer { Id = 501, Name = "SetNull Test" };
            var car = new Car { Id = 501, Name = "Orphan Car", Price = 25000, Manufacturer = manufacturer };
            
            _context.Manufacturers.Add(manufacturer);
            _context.Cars.Add(car);
            _context.SaveChanges();

            _context.Manufacturers.Remove(manufacturer);
            _context.SaveChanges();

            // Verify car still exists but without manufacturer
            var carQuery = _context.Query<Car>();
            var orphanCar = carQuery.Where("Id", "=", "501").Execute().FirstOrDefault();
            Console.WriteLine($"Orphan car manufacturer: {(orphanCar?.Manufacturer == null ? "Null" : "Not Null")}");
        }

        private void TestRelationshipLoading()
        {
            Console.WriteLine("\nTesting Relationship Loading...");
            
            // Create test data
            var manufacturer = new Manufacturer { Id = 601, Name = "Loading Test" };
            var car1 = new Car { Id = 601, Name = "Load Car 1", Price = 30000, Manufacturer = manufacturer };
            var car2 = new Car { Id = 602, Name = "Load Car 2", Price = 40000, Manufacturer = manufacturer };

            _context.Manufacturers.Add(manufacturer);
            _context.Cars.Add(car1);
            _context.Cars.Add(car2);
            _context.SaveChanges();

            // Test loading from both sides of the relationship
            var manuQuery = _context.Query<Manufacturer>();
            var loadedManu = manuQuery.Where("Id", "=", "601").Execute().FirstOrDefault();
            
            var carQuery = _context.Query<Car>();
            var loadedCar = carQuery.Where("Id", "=", "601").Execute().FirstOrDefault();

            Console.WriteLine($"Manufacturer cars count: {loadedManu?.Cars.Count ?? 0}");
            Console.WriteLine($"Car's manufacturer name: {loadedCar?.Manufacturer?.Name}");
        }
    }
} 