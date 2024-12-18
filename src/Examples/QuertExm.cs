using MyORM.Attributes;
using MyORM.Core;
using MyORM.Examples;

namespace MyORM.Examples
{
    [Table("Manufacturers")]
    public class Manufacturer : Entity
    {
        [Key(isAutoIncrement: false)]
        [Column("Id", false)]
        public int Id { get; set; }

        [Column("Name", false)]
        public string Name { get; set; }

        [Relationship(RelationType.OneToMany, typeof(Car), onDelete: DeleteBehavior.SetNull)]
        public virtual ICollection<Car> Cars { get; set; } = new List<Car>();
    }

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

        [Relationship(RelationType.ManyToOne, typeof(Manufacturer))]
        public virtual Manufacturer Manufacturer { get; set; }
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
        public DbSet<Manufacturer> Manufacturers { get; set; }

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

            // Create manufacturers
            var volkswagen = new Manufacturer { Id = 101, Name = "Volkswagen Group" };
            var bmw = new Manufacturer { Id = 102, Name = "BMW Group" };
            var daimler = new Manufacturer { Id = 103, Name = "Daimler AG" };
            var toyota = new Manufacturer { Id = 104, Name = "Toyota Motor Corp" };

            carsExm.Manufacturers.Add(volkswagen);
            carsExm.Manufacturers.Add(bmw);
            carsExm.Manufacturers.Add(daimler);
            carsExm.Manufacturers.Add(toyota);

            // Add cars with their manufacturers
            carsExm.Cars.Add(new Car { Name = "Toyota Camry", Price = 25000, Id = 1, Manufacturer = toyota });
            carsExm.Cars.Add(new Car { Name = "BMW 3 Series", Price = 40000, Id = 2, Manufacturer = bmw });
            carsExm.Cars.Add(new Car { Name = "Mercedes C-Class", Price = 45000, Id = 3, Manufacturer = daimler });
            carsExm.Cars.Add(new Car { Name = "Audi A4", Price = 35000, Id = 4, Manufacturer = volkswagen });
            carsExm.Cars.Add(new Car { Name = "BMW 5 Series", Price = 55000, Id = 5, Manufacturer = bmw });
            carsExm.Cars.Add(new Car { Name = "Mercedes E-Class", Price = 60000, Id = 6, Manufacturer = daimler });

            carsExm.SaveChanges();
        }

        public void RunQueryExamples()
        {
            var queryBuilder = new CarQuery();

            // Query examples with relationships
            Console.WriteLine("Cars by BMW:");
            var bmwCars = queryBuilder
                .Where("Name", "=", "BMW 3 Series")
                .Execute();

            foreach (var car in bmwCars)
            {
                Console.WriteLine(car.ToString());
                Console.WriteLine($"- {car.Name}: ${car.Price}. p: {car.Price} {car.Manufacturer.Name}");
            }

            // Get all manufacturers and their cars
            Console.WriteLine("\nManufacturers and their cars:");
            // var manufacturers = queryBuilder.Manufacturers.AsQueryable().ToList();
            // foreach (var manufacturer in manufacturers)
            // {
            //     Console.WriteLine($"\n{manufacturer.Name}:");
            //     foreach (var car in manufacturer.Cars)
            //     {
            //         Console.WriteLine($"- {car.Name}: ${car.Price}");
            //     }
            // }
        }
    }
}

