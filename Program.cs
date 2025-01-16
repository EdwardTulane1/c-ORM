// using MyORM.Examples;
using MyORM.Tests;

namespace MyORM
// Tests 1 - 2 - 3 are working
// Test 4 - working
// Test 5 - working . but if not removed in catch the same error will be save in context and will throw the same error over and over
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting ORM Tests...\n");

            // Run Advanced Relationship Tests
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

            var test6 = new TestSetNullBehavior();
            test6.RunAllTests();

            Console.WriteLine("\nAll tests completed.");
        }
    }
}