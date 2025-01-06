using MyORM.Attributes;
using MyORM.Core;
using System;
using System.Linq;

namespace MyORM.Tests
{
    public class Test3_Queries
    {
        private readonly Test1 _context;

        public Test3_Queries()
        {
            _context = new Test1();
            SetupTestData();
        }

        public void RunAllTests()
        {
            Console.WriteLine("Running Query Tests...");
            
            TestBasicFiltering();
            TestMultipleConditions();
       
            
            Console.WriteLine("Query Tests completed.");
        }

        private void SetupTestData()
        {
            // Create test manufacturers
            var manufacturers = new[]
            {
                new Manufacturer { Id = 701, Name = "Query Test 1" },
                new Manufacturer { Id = 702, Name = "Query Test 2" }
            };

            // Create test cars with various prices
            var cars = new[]
            {
                new Car { Id = 701, Name = "Low Price Car", Price = 20000, Manufacturer = manufacturers[0] },
                new Car { Id = 702, Name = "Mid Price Car", Price = 35000, Manufacturer = manufacturers[0] },
                new Car { Id = 703, Name = "High Price Car", Price = 50000, Manufacturer = manufacturers[1] },
                new Car { Id = 704, Name = "Luxury Car", Price = 75000, Manufacturer = manufacturers[1] }
            };

            foreach (var m in manufacturers)
                _context.Manufacturers.Add(m);
            
            foreach (var c in cars)
                _context.Cars.Add(c);
            
            _context.SaveChanges();
        }

        private void TestBasicFiltering()
        {
            Console.WriteLine("\nTesting Basic Filtering...");
            
            var query = _context.Query<Car>();
            var expensiveCars = query
                .Where("Price", ">", "40000")
                .Execute();
            
            Console.WriteLine($"Number of expensive cars found: {expensiveCars.Count()}");
            foreach (var car in expensiveCars)
            {
                Console.WriteLine($"Car: {car.Name}, Price: {car.Price}");
            }
        }

        private void TestMultipleConditions()
        {
            Console.WriteLine("\nTesting Multiple Conditions...");
            
            var query = _context.Query<Car>();
            var midRangeCars = query
                .Where("Price", ">", "30000")
                .Where("Price", "<", "60000")
                .Execute();
            
            Console.WriteLine($"Number of mid-range cars found: {midRangeCars.Count()}");
            foreach (var car in midRangeCars)
            {
                Console.WriteLine($"Car: {car.Name}, Price: {car.Price}");
            }
        }

        // You can not query on nested relationships.
    }
} 