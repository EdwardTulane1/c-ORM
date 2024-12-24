// using MyORM.Examples;
using MyORM.Tests;

namespace MyORM
{
    public class Program
    {
        static void Main(string[] args)
        {
            //console.WriteLine("Starting ORM Tests...\n");

            // Run Basic CRUD Tests
            var test1 = new Test1_BasicCRUD();
            test1.RunAllTests();

            // Run Relationship Tests
            // var test2 = new Test2_Relationships();
            // test2.RunAllTests();

            // // Run Query Tests
            // var test3 = new Test3_Queries();
            // test3.RunAllTests();

            ////console.WriteLine("\nAll tests completed. Press any key to exit...");
            //console.ReadKey();
        }
    }
}