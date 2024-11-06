using MyORM.Examples;

namespace MyORM
{
    public class Program
    {
        static void Main(string[] args)
        {
            var example = new XmlOrmExample();
            example.RunExample();

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}