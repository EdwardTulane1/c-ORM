using MyORM.Attributes;
using MyORM.Core;
using System.Collections.Generic;

namespace MyORM.Tests
{
    [Table("Authors")]
    public class Author : Entity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Name")]
        public string Name { get; set; }

        [Relationship(RelationType.OneToMany, typeof(Book), onDelete: DeleteBehavior.SetNull)]
        public virtual ICollection<Book> Books { get; set; } = new List<Book>();
    }

    [Table("Books")]
    public class Book : Entity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Title")]
        public string Title { get; set; }

        [Relationship(RelationType.ManyToOne, typeof(Author), onDelete: DeleteBehavior.None)]
        public virtual Author Author { get; set; }
    }

    public class SetNullContext : DbContext
    {
        public static readonly string XmlStoragePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "XmlStorage"
        );

        public DbSet<Author> Authors { get; set; }
        public DbSet<Book> Books { get; set; }

        public SetNullContext() : base(XmlStoragePath) { }
    }
} 