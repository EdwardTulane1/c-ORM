Based on the README.md and codebase, here's an overview of the ORM implementation:

## Core Structure

1. **Entity System**
- Every database object inherits from `Entity` base class
- Entities have three states tracked by flags:
  - IsNew: Set on creation, cleared after first save
  - IsModified: Set when properties change
  - IsDeleted: Set when marked for deletion
- the flags are updated based on snapshot of the entity taken after the save. And compared to the previous snapshot.

2. **DbContext**
- Main entry point for database operations
- Users create their own context by inheriting from DbContext
- Automatically discovers and initializes DbSet properties
- Manages entity tracking and persistence

3. **DbSet**
- Represents a collection of entities (table)
- Handles CRUD operations for specific entity types
- Maintains entity state tracking

4. **Storage**
- Uses XML files as the underlying storage mechanism
- Each table gets its own XML file
- Only properties with [Column] or [Key] attributes are persisted

## Key Features

1. **Relationship Types**

```19:23:src/Tests/TestEntities.cs
        [Relationship(RelationType.ManyToMany, typeof(Course), onDelete: DeleteBehavior.None)]
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

        [Relationship(RelationType.OneToOne, typeof(StudentProfile), onDelete: DeleteBehavior.Orphan)]
        public virtual StudentProfile Profile { get; set; }
```

- One-to-Many
- Many-to-One
- Many-to-Many
- One-to-One

2. **Delete Behaviors**
- Cascade
- SetNull
- None
- Orphan

3. **Validation System**

```15:33:src/Examples/ValidationExample.cs
        [Required]
        [StringLength(20, 2)]
        [Column("Username", false)]
        public virtual string Username { get; set; }

        [Required]
        [Email]
        [Column("Email", false)]
        public virtual string Email { get; set  ;}

        [Required]
        [StringLength(50, 1)]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$")] // At least 8 characters, 1 letter and 1 number
        [Column("Password", false)]
        public virtual string Password { get; set; }

        [Range(2, 120)]
        [Column("Age", false)]
        public virtual int Age { get; set; }
```


## Limitations

1. **Performance**
- No optimization considerations
- Full table scans for queries
- No caching mechanism

2. **Relationship Constraints**
- Circular dependencies not supported
- One-to-one relations saved only on one side
- Relationships must use key-to-key mapping

3. **Change Tracking**
- No automatic property change detection
- Changes only tracked through SaveChanges()
- Related entities might have inconsistent states before save

4. **Technical Limitations**
- Single key per entity
- No support for complex queries
- No transaction support
- No concurrency handling

## Design Decisions

1. **Change Tracking**

```32:36:README.md
A strategic decision - to have event on modify or to live with @saveChanges
https://sampathdissanyake.medium.com/racking-data-changes-in-c-net-c2be9ed333fd


The options to better flow included using setPropert/OnPropertyChangedy on the user side itself, which I tried to avoid in order to make the system more user friendly. It means we don;t track changes in entities
```

- Opted for explicit SaveChanges() over property change events
- Prioritized user-friendliness over real-time tracking

2. **Relationship Handling**

```41:46:README.md
in savechanges related entity's might be changes while their related still don't know about it.

SO - a change doesnt matter. we only mapping unchangeable keys
only thing that matters deleting
when a entity is mapped to a already deleted one - we need to know about it and react to it.

```

- Delete operations are the primary concern in relationships
- Delete behavior defined on the deleted side for easier access
- Relationship mappings use only key attributes

3. **Storage Strategy**
- XML-based for simplicity and readability
- Separate files for each entity type
- Relationship data stored in dedicated files

This implementation prioritizes simplicity and ease of use over performance and advanced features, making it suitable for learning and small applications but not production use.
