using System;
using MyORM.Attributes;
using MyORM.Core;

namespace MyORM.Examples
{
    // Define entities
    [Table("Customers")]
    public class Customer : Entity
    {
        [Key(isAutoIncrement: false)]
        [Column("Id", false)]
        public int Id { get; set; }

        [Column("FirstName", false)]
        public string FirstName { get; set; }

        [Column("LastName", false)]
        public string LastName { get; set; }
    }

    [Table("Orders")]
    public class Order : Entity
    {
        [Key(isAutoIncrement: false)]
        [Column("Id", false)]
        public int Id { get; set; }

        [Column("OrderDate", false)]
        public DateTime OrderDate { get; set; }

        [Column("CustomerId", false)]
        public int CustomerId { get; set; }

        [Relationship(RelationType.ManyToOne, typeof(Customer), "CustomerId")]
        public virtual Customer Customer { get; set; }
    }

    // Define context
    public class MyXmlContext : DbContext
    {
        public static readonly string XmlStoragePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "XmlStorage"
        );

        public DbSet<Customer> Customers { get; private set; }
        public DbSet<Order> Orders { get; private set; }

        public MyXmlContext() : base(XmlStoragePath)
        {
            Console.WriteLine($"XML files will be stored in: {XmlStoragePath}");
        }
    }

    public class XmlOrmExample
    {
        public void RunExample()
        {
            Console.WriteLine("XML ORM Example Application");
            Console.WriteLine("-------------------------");
            Console.WriteLine($"Storage Path: {MyXmlContext.XmlStoragePath}");

            using (var context = new MyXmlContext())
            {
                // Example 1: Adding a new customer
                var customer = new Customer
                {
                    Id = 1,
                    FirstName = "John",
                    LastName = "Doe"
                };
                context.Customers.Add(customer);
                Console.WriteLine($"Added customer: {customer.FirstName} {customer.LastName}");

                // Example 2: Adding an order for the customer
                var order = new Order
                {
                    Id = 1,
                    OrderDate = DateTime.Now,
                    CustomerId = customer.Id,
                    Customer = customer
                };
                context.Orders.Add(order);
                Console.WriteLine($"Added order for customer: {order.Id}");

                // Save all changes to XML
                context.SaveChanges();
                Console.WriteLine("Changes saved to XML files");

                // Example 3: Modifying a customer
                customer.LastName = "Smith";
                Console.WriteLine($"Modified customer last name to: {customer.LastName}");
                context.SaveChanges();
                Console.WriteLine("Changes saved to XML files");

                // Example 4: Removing an order
                // context.Orders.Remove(order);
                // Console.WriteLine($"Removed order: {order.Id}");
                // context.SaveChanges();
                // Console.WriteLine("Changes saved to XML files");

                // Example 5: Query (Note: Basic in-memory query)
                var customers = context.Customers.AsQueryable()
                    .Where(c => c.LastName == "Smith")
                    .ToList();
                
                Console.WriteLine("\nQuerying customers with last name 'Smith':");
                foreach (var c in customers)
                {
                    Console.WriteLine($"Found customer: {c.FirstName} {c.LastName}");
                }
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}

