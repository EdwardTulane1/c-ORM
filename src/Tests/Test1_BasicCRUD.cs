using MyORM.Attributes;
using MyORM.Core;
using System;
using System.Linq;
using System.IO;

// working!
namespace MyORM.Tests
{
    public class Test1_BasicCRUD
    {
        private readonly Test1 _context;
        private readonly string _testStoragePath;

        public Test1_BasicCRUD()
        {
            _testStoragePath = Path.Combine(Directory.GetCurrentDirectory(), "TestStorage");
            _context = new Test1();
        }

        public void RunAllTests()
        {
            Console.WriteLine("Running Basic CRUD Tests...");
            
            TestCreate();
            TestRead();
            TestUpdate();
            TestDelete();
            
            Console.WriteLine("Basic CRUD Tests completed.");
        }

        private void TestCreate()
        {
            Console.WriteLine("\nTesting Create...");
            
            // Test single entity creation
            var manufacturer = new Manufacturer { Id = 201, Name = "Test Manufacturer" };
            _context.Manufacturers.Add(manufacturer);
            _context.SaveChanges();


            // Test entity with relationship creation
            var car = new Car 
            { 
                Id = 201, 
                Name = "Test Car", 
                Price = 25000, 
                Manufacturer = manufacturer 
            };
            _context.Cars.Add(car);
            _context.SaveChanges();

            // Verify
            var query = _context.Query<Car>();
            var saved = query.Where("Id", "=", "201").Execute().FirstOrDefault();
            Console.WriteLine($"Created manufacturer found: {saved?.Name ?? "Not found"}/ Manufacturer: {saved?.Manufacturer?.Name}. ");
        }

        private void TestRead()
        {
            Console.WriteLine("\nTesting Read...");
            
            var carQuery = _context.Query<Car>();
            var car = carQuery.Where("Id", "=", "201").Execute().FirstOrDefault();
            
            Console.WriteLine($"Read car details: {car?.Name}, Price: {car?.Price}. isnew: {car?.IsNew}");
            Console.WriteLine($"Associated manufacturer: {car?.Manufacturer?.Name}");
        }

        // BIG TODO - same problem on update like in delete. not tracked by context
        // The delete is not a solution. when reading from db (quering) I have to add them for tracking
        private void TestUpdate()
        {
            Console.WriteLine("\nTesting Update...");
            
            var query = _context.Query<Manufacturer>();
            var manufacturer = query.Where("Id", "=", "201").Execute().FirstOrDefault();
            
            if (manufacturer != null)
            {
                manufacturer.Name = "Updated Manufacturer";
                _context.SaveChanges();                
                // Verify update
                var updated = query.Where("Id", "=", "201").Execute().FirstOrDefault();
                Console.WriteLine($"Updated name: {updated?.Name}");
            }
        }

        private void TestDelete()
        {
            Console.WriteLine("\nTesting Delete...");
            
            // Create temporary entity to delete
            var tempManufacturer = new Manufacturer { Id = 999, Name = "To Be Deleted" };
            _context.Manufacturers.Add(tempManufacturer);
            _context.SaveChanges();
            
            // Delete the entity
            _context.Manufacturers.Remove(tempManufacturer);
            _context.SaveChanges();
            
            // Verify deletion
            var query = _context.Query<Manufacturer>();
            var deleted = query.Where("Id", "=", "999").Execute().FirstOrDefault();
            Console.WriteLine($"Deleted manufacturer found: {(deleted == null ? "No" : "Yes")}");
        }
    }
} 