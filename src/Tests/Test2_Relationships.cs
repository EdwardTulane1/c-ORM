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
            TestRelationshipLoading();
            
            _context.Dispose();
            Console.WriteLine("Relationship Tests completed.");
        }

        private void TestOneToManyRelationship()
        {
            Console.WriteLine("\nTesting One-to-Many Relationship...");
            
            var manufacturer = new Manufacturer { Id = 301, Name = "BMW Test" };
            var car1 = new Car { Id = 401, Name = "Test Car 1", Price = 30000, Manufacturer = manufacturer };
            var car2 = new Car { Id = 402, Name = "Test Car 2", Price = 40000, Manufacturer = manufacturer };

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

            var manufacturer = new Manufacturer { Id = 310, Name = "Cascade Test" };
            var manufacturer2 = new Manufacturer { Id = 311, Name = "Cascade Test 2" };
            var car = new Car { Id = 410, Name = "Cascade Car", Price = 50000, Manufacturer = manufacturer };
            _context.Manufacturers.Add(manufacturer);
            _context.Manufacturers.Add(manufacturer2);
            _context.Cars.Add(car);
            _context.SaveChanges();

            // car.Manufacturer = manufacturer2;
            // _context.SaveChanges();

            var carQuery = _context.Query<Car>();

            var deletedCar1 = carQuery.Where("Id", "=", "410").Execute().FirstOrDefault();
            Console.WriteLine($"Deleted car 1: {deletedCar1?.Name}, {deletedCar1?.Manufacturer?.Name}");

            _context.Manufacturers.Remove(manufacturer);
            _context.SaveChanges();

            // Verify both manufacturer and car are deleted
            var deletedCar = carQuery.Where("Id", "=", "410").Execute().FirstOrDefault();
            Console.WriteLine($"Cascade deleted car found: {(deletedCar == null ? "No" : "Yes")}");
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