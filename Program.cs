// using MyORM.Tests;
using MyORM.UsageExample;
namespace MyORM

{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting ORM Tests...\n");

            // var test1 = new Test1_BasicCRUD(); // - works
            // test1.RunAllTests();

            // var test2 = new Test2_Relationships();
            // test2.RunAllTests();

            // var test3 = new Test3_Queries();
            // test3.RunAllTests();

            // var test4 = new Test4_AdvancedRelationships();
            // test4.RunAllTests();

            // var test5 = new Test5_Validation();
            // test5.RunAllTests();

            // var test6 = new TestSetNullBehavior();
            // test6.RunAllTests();

            var example = new BookstoreExample();
            example.RunExample();

            Console.WriteLine("\nAll tests completed.");
        }
    }
}