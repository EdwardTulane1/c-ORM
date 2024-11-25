using System;
using System.IO;
using System.Linq;
using MyORM.Attributes;
using MyORM.Core;

namespace MyORM.Examples
{
    // Define entities
    [Table("Customers")]
    public class CustomerWithOrders : Entity
    {
        [Key(isAutoIncrement: false)]
        [Column("Id", false)]
        public int Id { get; set; }

        [Column("FirstName", false)]
        public string FirstName { get; set; }

        [Column("LastName", false)]
        public string LastName { get; set; }

        [Relationship(RelationType.OneToMany, typeof(OrderWithCustomer), fromProperty: "Id", toProperty: "CustomerId", onDelete: DeleteBehavior.SetNull)]
        public virtual ICollection<OrderWithCustomer> Orders { get; set; } = new List<OrderWithCustomer>();
    }

    [Table("Categories")]
    public class Category : Entity
    {
        [Key(isAutoIncrement: false)]
        [Column("Id", false)]
        public int Id { get; set; }

        [Column("Name", false)]
        public string Name { get; set; }

        [Relationship(RelationType.ManyToMany, typeof(OrderWithCustomer), fromProperty: "Id", toProperty: "Id")]
        public virtual ICollection<OrderWithCustomer> Orders { get; set; } = new List<OrderWithCustomer>();
    }

    [Table("Orders")]
    public class OrderWithCustomer : Entity
    {
        [Key(isAutoIncrement: false)]
        [Column("Id", false)]
        public int Id { get; set; }

        [Column("OrderDate", false)]
        public DateTime OrderDate { get; set; }

        [Column("CustomerId", false)]
        public int CustomerId { get; set; }

        [Relationship(RelationType.ManyToOne, typeof(CustomerWithOrders), fromProperty: "CustomerId", toProperty: "Id")]
        public virtual CustomerWithOrders Customer { get; set; }

        [Relationship(RelationType.ManyToMany, typeof(Category), fromProperty: "Id", toProperty: "Id")]
        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
    }

    // Define context
    public class MyXmlContextWithRelations : DbContext
    {
        public static readonly string XmlStoragePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "XmlStorage"
        );

        public DbSet<CustomerWithOrders> Customers { get; private set; }
        public DbSet<OrderWithCustomer> Orders { get; private set; }
        public DbSet<Category> Categories { get; private set; }

        public MyXmlContextWithRelations() : base(XmlStoragePath)
        {
            Console.WriteLine($"XML files will be stored in: {XmlStoragePath}");
        }
    }

    public class XmlOrmExampleWithRelations
    {
        public void RunExample()
        {
            Console.WriteLine("XML ORM Example with Relations");
            Console.WriteLine("-------------------------");
            Console.WriteLine($"Storage Path: {MyXmlContextWithRelations.XmlStoragePath}");

            using (var context = new MyXmlContextWithRelations())
            {
                // Create categories
                var electronics = new Category { Id = 1, Name = "Electronics" };
                var books = new Category { Id = 2, Name = "Books" };
                
                context.Categories.Add(electronics);
                context.Categories.Add(books);
                Console.WriteLine($"Added categories: Electronics, Books");

                // Example 1: Adding a new customer
                var johnCustomer = new CustomerWithOrders
                {
                    Id = 1,
                    FirstName = "John",
                    LastName = "Doe"
                };
                context.Customers.Add(johnCustomer);
                Console.WriteLine($"Added customer: {johnCustomer.FirstName} {johnCustomer.LastName}");

                var dannielCustomer = new CustomerWithOrders
                {
                    Id = 2,
                    FirstName = "Danniel",
                    LastName = "Smith"
                };

                context.Customers.Add(dannielCustomer);
                Console.WriteLine($"Added customer: {dannielCustomer.FirstName} {dannielCustomer.LastName}");
                
                // Example 2: Adding an order for the customer
                var order = new OrderWithCustomer
                {
                    Id = 1,
                    OrderDate = DateTime.Now,
                    CustomerId = johnCustomer.Id,
                    Customer = johnCustomer
                };
                // Add categories to order
                order.Categories.Add(electronics);
                order.Categories.Add(books);
                context.Orders.Add(order);
                Console.WriteLine($"Added order with categories for customer: {order.Id}");

                var order2 = new OrderWithCustomer
                {
                    Id = 2,
                    OrderDate = DateTime.Now,
                    CustomerId = dannielCustomer.Id,
                    Customer = dannielCustomer
                };
                // Add category to second order
                order2.Categories.Add(books);
                context.Orders.Add(order2);
                Console.WriteLine($"Added order with category for customer: {order2.Id}");

                // Save all changes to XML
                context.SaveChanges();
                Console.WriteLine("Changes saved to XML files");

                // Example 3: Modifying a customer
                johnCustomer.LastName = "Smith";
                Console.WriteLine($"Modified customer last name to: {johnCustomer.LastName}");
                context.SaveChanges();
                Console.WriteLine("Changes saved to XML files");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}

