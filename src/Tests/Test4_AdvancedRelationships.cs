using MyORM.Attributes;
using MyORM.Core;
using System;
using System.Linq;

namespace MyORM.Tests
{
    public class Test4_AdvancedRelationships
    {
        private readonly TestContext _context;

        public Test4_AdvancedRelationships()
        {
            _context = new TestContext();
        }

        public void RunAllTests()
        {
            Console.WriteLine("Running Advanced Relationship Tests...");
            
            TestManyToManyCreate();
            TestManyToManyQuery();
            TestManyToManyDelete();
            TestOneToOneCreate();
            TestOneToOneQuery();
            TestOneToOneDelete();
            
            Console.WriteLine("Advanced Relationship Tests completed.");
        }

        private void TestManyToManyCreate()
        {
            Console.WriteLine("\nTesting Many-to-Many Creation...");
            
            var math = new Course { Id = 101, Name = "Mathematics" };
            var physics = new Course { Id = 102, Name = "Physics" };
            
            var student1 = new Student { Id = 10, Name = "John Doe" };
            var student2 = new Student { Id = 11, Name = "Jane Smith" };

            // Create relationships
            student1.Courses.Add(math);
            student1.Courses.Add(physics);
            student2.Courses.Add(math);

            _context.Students.Add(student1);
            _context.Students.Add(student2);
            _context.Courses.Add(math);
            _context.Courses.Add(physics);
            _context.SaveChanges();

            // Verify
            var mathCourse = _context.Query<Course>()
                .Where("Id", "=", "101")
                .Execute()
                .FirstOrDefault();

            Console.WriteLine($"Math course student count: {mathCourse?.Students.Count ?? 0}. Course is : {mathCourse?.Name}");
        }

        private void TestManyToManyQuery()
        {
            Console.WriteLine("\nTesting Many-to-Many Query...");
            
            var student = _context.Query<Student>()
                .Where("Id", "=", "10")
                .Execute()
                .FirstOrDefault();

            Console.WriteLine($"Student courses count: {student?.Courses.Count ?? 0}");
            foreach (var course in student?.Courses ?? Enumerable.Empty<Course>())
            {
                Console.WriteLine($"Course: {course.Name}");
            }
        }

        private void TestManyToManyDelete()
        {
            Console.WriteLine("\nTesting Many-to-Many Delete...");
            
            var course = _context.Query<Course>()
                .Where("Id", "=", "101")
                .Execute()
                .FirstOrDefault();

            if (course != null)
            {
                _context.Courses.Remove(course);
                Console.WriteLine($"Removing course: {course.Name}");
                _context.SaveChanges();
                Console.WriteLine("Course removed");

                // Verify students still exist but course is removed from their collections
                var student = _context.Query<Student>()
                    .Where("Id", "=", "10")
                    .Execute();

                Console.WriteLine($"Student courses after delete: {student?.Count ?? 0}");
            }
        }

        private void TestOneToOneCreate()
        {
            Console.WriteLine("\nTesting One-to-One Creation...");
            
            var student = new Student { Id = 21, Name = "Bob Wilson" };
            var profile = new StudentProfile 
            { 
                Id = 201, 
                Email = "bob@example.com",
                Student = student 
            };

            student.Profile = profile;

            _context.Students.Add(student);
            _context.StudentProfiles.Add(profile);
            _context.SaveChanges();

            // Verify
            var savedStudent = _context.Query<Student>()
                .Where("Id", "=", "201")
                .Execute()
                .FirstOrDefault();

            Console.WriteLine($"Student profile email: {savedStudent?.Profile?.Email}");
        }

        private void TestOneToOneQuery()
        {
            Console.WriteLine("\nTesting One-to-One Query...");
            
            // Query from both sides of the relationship
            var profile = _context.Query<StudentProfile>()
                .Where("Id", "=", "201")
                .Execute()
                .FirstOrDefault();

            var student = _context.Query<Student>()
                .Where("Id", "=", "21")
                .Execute()
                .FirstOrDefault();

            Console.WriteLine($"Profile -> Student name: {profile?.Student?.Name}. ");
            Console.WriteLine($"Student -> Profile email: {student?.Profile?.Email}");
        }

        private void TestOneToOneDelete()
        {
            Console.WriteLine("\nTesting One-to-One Delete...");
            
            var student = _context.Query<Student>()
                .Where("Id", "=", "21")
                .Execute()
                .FirstOrDefault();

            if (student != null)
            {
                // _context.Students.Remove(student);
                // _context.SaveChanges();

                // Verify profile is also deleted (cascade behavior)
                var profile = _context.Query<StudentProfile>()
                    .Where("Id", "=", "201")
                    .Execute()
                    .FirstOrDefault();

                if (profile != null)
                {
                    _context.StudentProfiles.Remove(profile);
                    _context.SaveChanges();
                }

                Console.WriteLine($"Profile after student delete exists: {profile != null}");
            }
        }
    }
} 