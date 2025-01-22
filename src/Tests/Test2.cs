using MyORM.Attributes;
using MyORM.Core;

namespace MyORM.Tests
{
    [Table("Manufacturers")]
    public class Manufacturer : Entity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Name")]
        public string Name { get; set; }

        [Relationship(RelationType.OneToMany, typeof(Car), onDelete: DeleteBehavior.Cascade)]
        public virtual ICollection<Car> Cars { get; set; } = new List<Car>();
    }

    [Table("Cars")]
    public class Car : Entity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Name")]
        public string Name { get; set; }

        [Column("Price")]
        public int Price { get; set; }

        [Relationship(RelationType.ManyToOne, typeof(Manufacturer), onDelete:DeleteBehavior.None)]
        public virtual Manufacturer Manufacturer { get; set; }
    }




    public class Test1 : DbContext
    {

        public DbSet<Car> Cars { get; set; }
        public DbSet<Manufacturer> Manufacturers { get; set; }

        public Test1() : base()
        {
        }
    }

    public class Test
    {
        public void createDataForQuery()
        {
            var test1 = new Test1();

            var volkswagen = new Manufacturer { Id = 101, Name = "Volkswagen Group" };
            var bmw = new Manufacturer { Id = 102, Name = "BMW Group" };
            var daimler = new Manufacturer { Id = 103, Name = "Daimler AG" };
            var toyota = new Manufacturer { Id = 104, Name = "Toyota Motor Corp" };

            test1.Manufacturers.Add(volkswagen);
            test1.Manufacturers.Add(bmw);
            test1.Manufacturers.Add(daimler);
            test1.Manufacturers.Add(toyota);

            // Add cars with their manufacturers
            test1.Cars.Add(new Car { Name = "Toyota Camry", Price = 25000, Id = 1, Manufacturer = toyota });
            test1.Cars.Add(new Car { Name = "BMW 3 Series", Price = 40000, Id = 2, Manufacturer = bmw });
            test1.Cars.Add(new Car { Name = "Mercedes C-Class", Price = 45000, Id = 3, Manufacturer = daimler });
            test1.Cars.Add(new Car { Name = "Audi A4", Price = 35000, Id = 4, Manufacturer = volkswagen });
            test1.Cars.Add(new Car { Name = "BMW 5 Series", Price = 55000, Id = 5, Manufacturer = bmw });
            test1.Cars.Add(new Car { Name = "Mercedes E-Class", Price = 60000, Id = 6, Manufacturer = daimler });

            test1.SaveChanges();


         

        }

        // public void runQuery()
        // {
        //     var test1 = new Test1();
            
        //     // Use the context's Query method instead of separate query builders
        //     var bmwCars = test1.Query<Car>()
        //         .Where("Name", "=", "BMW 3 Series")
        //         .Execute();

        //     foreach (var car in bmwCars)
        //     {
        //         Console.WriteLine($"- {car.Name}: ${car.Price}. Manufacturer: {car.Manufacturer.Name}");
        //     }

        //     // Query manufacturers with their related cars
        //     var manufacturers = test1.Query<Manufacturer>().Execute();
        //     foreach (var manufacturer in manufacturers)
        //     {
        //         Console.WriteLine(manufacturer.Name);
        //         foreach (var car in manufacturer.Cars)
        //         {
        //             Console.WriteLine($" - {car.Name}: ${car.Price}");
        //         }
        //     }

        //     // Example of querying and then modifying tracked entities
        //     var volkswagen = test1.Query<Manufacturer>()
        //         .Where("Name", "=", "Volkswagen Group")
        //         .Execute()
        //         .FirstOrDefault();

        //     if (volkswagen != null)
        //     {
        //         Console.WriteLine($"Before delete: {volkswagen.Name}, {volkswagen.Id}");
        //         test1.Manufacturers.Remove(volkswagen);
        //         test1.SaveChanges();
        //         Console.WriteLine("After delete");
        //     }
        // }
    }

}



// TEsts To run -  running twice generating error due to violating key uniqness - works
