using MyORM.Attributes;
using MyORM.Core;


namespace MyORM.Tests
{
    [Table("Students")]
    public class Student : Entity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Name")]
        public string Name { get; set; }

        [Relationship(RelationType.ManyToMany, typeof(Course), onDelete: DeleteBehavior.None)]
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

        [Relationship(RelationType.OneToOne, typeof(StudentProfile), onDelete: DeleteBehavior.Orphan)]
        public virtual StudentProfile Profile { get; set; }
    }

    [Table("Courses")]
    public class Course : Entity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Name")]
        public string Name { get; set; }

        [Relationship(RelationType.ManyToMany, typeof(Student), onDelete: DeleteBehavior.None)]
        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
    }

    [Table("StudentProfiles")]
    public class StudentProfile : Entity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Email")]
        public string Email { get; set; }

        [Relationship(RelationType.OneToOne, typeof(Student), onDelete: DeleteBehavior.SetNull)]
        public virtual Student Student { get; set; }
    }

    public class TestContext : DbContext
    {
      
        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<StudentProfile> StudentProfiles { get; set; }

        public TestContext() : base()
        {
        }
    }
} 