using MyORM.Attributes;
using MyORM.Core;

using System;
using System.IO;

namespace MyORM.Tests
{
    [Table("Students")]
    public class Student : Entity
    {
        [Key(isAutoIncrement: false)]
        [Column("Id", false)]
        public int Id { get; set; }

        [Column("Name", false)]
        public string Name { get; set; }

        [Relationship(RelationType.ManyToMany, typeof(Course), onDelete: DeleteBehavior.None)]
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

        [Relationship(RelationType.OneToOne, typeof(StudentProfile), onDelete: DeleteBehavior.SetNull)]
        public virtual StudentProfile Profile { get; set; }
    }

    [Table("Courses")]
    public class Course : Entity
    {
        [Key(isAutoIncrement: false)]
        [Column("Id", false)]
        public int Id { get; set; }

        [Column("Name", false)]
        public string Name { get; set; }

        [Relationship(RelationType.ManyToMany, typeof(Student), onDelete: DeleteBehavior.None)]
        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
    }

    [Table("StudentProfiles")]
    public class StudentProfile : Entity
    {
        [Key(isAutoIncrement: false)]
        [Column("Id", false)]
        public int Id { get; set; }

        [Column("Email", false)]
        public string Email { get; set; }

        [Relationship(RelationType.OneToOne, typeof(Student), onDelete: DeleteBehavior.Orphan)]
        public virtual Student Student { get; set; }
    }

    public class TestContext : DbContext
    {
        public static readonly string XmlStoragePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "XmlStorage"
        );

        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<StudentProfile> StudentProfiles { get; set; }

        public TestContext() : base(XmlStoragePath)
        {
            Console.WriteLine($"XML files will be stored in: {XmlStoragePath}");
        }
    }
} 