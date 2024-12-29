// using MyORM.Examples;
using MyORM.Tests;

namespace MyORM
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting ORM Tests...\n");

            // Run Advanced Relationship Tests
            var test4 = new Test4_AdvancedRelationships();
            test4.RunAllTests();

            Console.WriteLine("\nAll tests completed.");
        }
    }
}