using MyORM.Attributes;
using MyORM.Core;
using MyORM.Examples;

namespace MyORM.Examples
{
    [Table("Cars")]
    public class Car : Entity
    {
        [Key(isAutoIncrement: false)]
        [Column("Id", false)]
        public int Id { get; set; }

        [Column("Name", false)]
        public string Name { get; set; }

        [Column("Price", false)]
        public int Price { get; set; }
    }

    public class CarQuery : XmlQueryBuilder<Car>
    {
        public CarQuery() : base(carsExm.XmlStoragePath, "Car")
        {
            Console.WriteLine($"XML files will be stored in: {carsExm.XmlStoragePath}");
        }
    }

    public class carsExm : DbContext
    {
        public static readonly string XmlStoragePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "XmlStorage"
        );

        public DbSet<Car> Cars { get; set; }

        public carsExm() : base(XmlStoragePath)
        {
            Console.WriteLine($"XML files will be stored in: {XmlStoragePath}");
        }
    }


    public class CarQueryExample
    {
        public void createDataForQuery()
        {
            var carsExm = new carsExm();
            carsExm.Cars.Add(new Car { Name = "Toyota", Price = 25000 , Id = 1});
            carsExm.Cars.Add(new Car { Name = "Honda", Price = 20000 , Id = 2});
            carsExm.Cars.Add(new Car { Name = "Ford", Price = 30000 , Id = 3});
            carsExm.Cars.Add(new Car { Name = "Chevrolet", Price = 28000 , Id = 4});
            carsExm.Cars.Add(new Car { Name = "Volkswagen", Price = 26000 , Id = 5});
            carsExm.Cars.Add(new Car { Name = "Audi", Price = 35000 , Id = 6});
            carsExm.Cars.Add(new Car { Name = "BMW", Price = 40000 , Id = 7});
            carsExm.Cars.Add(new Car { Name = "Mercedes", Price = 45000 , Id = 8});
            carsExm.Cars.Add(new Car { Name = "Volvo", Price = 32000 , Id = 9});
            carsExm.Cars.Add(new Car { Name = "Skoda", Price = 120000 , Id = 10});
            carsExm.SaveChanges();
        }
        public void RunQueryExamples()
        {
            // Initialize the query builder with a base path and table name
            var queryBuilder = new CarQuery();

            // Example 1: Basic Where query
            var expensiveCars = queryBuilder
                .Where("Price", ">", 50000)
                .Execute();
            
            Console.WriteLine("Expensive cars (>$50,000):");
            foreach (var car in expensiveCars)
            {
                Console.WriteLine($"- {car.Name}: ${car.Price}");
            }

            // Example 2: Multiple conditions with ordering
            var luxuryCars = queryBuilder
                .Where("Price", ">", 80000)
                .Where("Name", "LIKE", "BMW")
                .OrderBy("Price", descending: true)
                .Execute();

            Console.WriteLine("\nLuxury BMW cars ordered by price (descending):");
            foreach (var car in luxuryCars)
            {
                Console.WriteLine($"- {car.Name}: ${car.Price}");
            }

            // Example 3: Pagination
            var pagedCars = queryBuilder
                .OrderBy("Name")
                .Skip(2)
                .Take(3)
                .Execute();

            Console.WriteLine("\nPaged cars (skip 2, take 3):");
            foreach (var car in pagedCars)
            {
                Console.WriteLine($"- {car.Name}: ${car.Price}");
            }

            // Example 4: Combining multiple operations
            var filteredCars = queryBuilder
                .Where("Price", "<", 100000)
                .Where("Name", "LIKE", "Mercedes")
                .OrderBy("Price")
                .Skip(1)
                .Take(5)
                .Execute();

            Console.WriteLine("\nFiltered Mercedes cars (Price < $100,000, ordered, paged):");
            foreach (var car in filteredCars)
            {
                Console.WriteLine($"- {car.Name}: ${car.Price}");
            }
        }
    }
}

