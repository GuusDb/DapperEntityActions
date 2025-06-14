# CustomDapperEntityActions

A .NET package that provides Entity Framework Core-like actions using Dapper, designed for developers whose companies restrict the use of EF Core.

## Overview

CustomDapperEntityActions bridges the gap between Dapper's lightweight performance and Entity Framework Core's convenient entity management features. It offers familiar entity action patterns while maintaining Dapper's speed and flexibility.

## Features

- EF Core-style entity operations
- Built on top of Dapper for optimal performance
- Compatible with .NET 9.0
- Support for common CRUD operations

## Installation

Install via NuGet Package Manager:
dotnet add package CustomDapperEntityActions --version 1.0.6

Link to nuget: https://www.nuget.org/packages/CustomDapperEntityActions/

## How to use

1. Define entities

```C#
[Table("[schema].[table]")]
public class Plant
{
   [Key]
   [Column("id")]
   public int Id { get; set; }

   [Column("description")]
   public string? Description { get; set; }

   [Column("is_active")]
   public bool IsAcive { get; set; }
}
```

1.1 Define Entities with 1..n relationship
```C#
// Parent table
[Table("parent")]
public class Parent
{
    [Key]
    [Column("parent_id")]
    public int ParentId { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    [NotMapped]
    public List<Child> Children { get; set; } = new List<Child>(); // initialise is important and needed 
}

// Child table
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

[Table("child")]
public class Child
{
    [Key]
    [Column("child_id")]
    public int ChildId { get; set; }

    // foreign key to parent necessary
    [Column("parent_id")]
    [ForeignKey("Parent")]
    public int ParentId { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }
}
```

2. Create DbContext

```C#
public class ApplicationDbContext : IDisposable
{
    // if using postgres, sql also possible
    private readonly NpgsqlConnection _connection;
    private IDbTransaction _transaction;

    // The entity we just made
    public DapperSet<Plant> Plants { get; }


    public ApplicationDbContext(string connectionString)
    {
        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();
        _transaction = _connection.BeginTransaction();
        // assign them
        Plants = new DapperSet<Plant>(_connection, _transaction);
    }

    public void Commit() => _transaction?.Commit();
    public void Rollback() => _transaction?.Rollback();

    public void Dispose()
    {
        _transaction?.Dispose();
        _connection?.Dispose();
    }
}
```

3.  Update program.cs

```C#
bld.Services.AddScoped<ApplicationDbContext>(provider => new ApplicationDbContext(connectionString));
```

4. usage

```C#
// insert
var insert = await dbContext.Tests.InsertAsync<string>(new TestLalala
{
    Description = "Test",
    IsAcive = true
});

// update
var update = await dbContext.Tests.UpdateAsync(new TestLalala
{
    Id = 5,
    Description = "Luksass",
    IsAcive = true
});
// Get byid
await dbContext.Tests.GetByIdAsync(5);
await dbContext.Tests.DeleteAsync(5);

// IMPORTANT: You must call Commit() to persist changes to the database
// after Insert, Update, or Delete operations
dbContext.Commit();

// Linq like query NEW version
     var specificTests = await dbContext.Tests
         .Where(x => x.TestCd == "Zoko")
         .OrderBy(x => x.Description)
         .ExecuteAsync();
// without where also possible
 var specificTests = await dbContext.Tests
         .OrderBy(x => x.Description)
         .ExecuteAsync();
// only where also possible
     var specificTests = await dbContext.Tests
         .Where(x => x.TestCd == "Zoko")
// descending
 var specificTests = await dbContext.Tests
         .OrderBy(x => x.Description, descending: true)
         .ExecuteAsync();


// Chaining statements
 var specificTests = await dbContext.Tests
     .Where( x => x.TestMode == "Offline")
     .Where(x => x.IsActive )
     .OrderBy(x => x.TestType)
     .OrderBy( x => x.TestMode)
     .ExecuteAsync();



// Linq like query OLD version
var specificTests = await dbContext.Tests.WhereAsync(x =>x.IsAcive );

// GetAll
DbContext.Tests.GetAllAsync

```
## Persisting Changes

When using Insert, Update, or Delete operations, you must call `dbContext.Commit()` to persist your changes to the database:

```C#
// Insert a new entity
await dbContext.Plants.InsertAsync<string>(new Plant {
    PlantCd = "PLANT5",
    Description = "New Plant",
    IsAcive = true
});

// Update an existing entity
await dbContext.Plants.UpdateAsync(new Plant {
    PlantCd = "PLANT5",
    Description = "Updated Plant",
    IsAcive = false
});

// Delete an entity
await dbContext.Plants.DeleteAsync<string>("PLANT5");

// Commit the transaction to persist all changes
dbContext.Commit();

// If you need to rollback changes instead
// dbContext.Rollback();
```

The library uses transactions to ensure data consistency. Without calling `Commit()`, your changes will be rolled back when the DbContext is disposed.

## Pagination
```C#
  var specificTests = await dbContext.Tests
           // Other linq queries
            .Paginate(0,50) // paginate(pageIndex, pageSize), this gives records 0-50
            .ExecuteAsync();
```

## Select
The Select feature allows you to project entities to a different type, similar to Entity Framework Core's Select method. This optimizes your queries by only retrieving the columns you need from the database.

```C#
// Select specific properties
var descriptions = await dbContext.Plants
    .Select(p => p.Description)
    .ExecuteAsync();

// Project to an anonymous type
var plantInfo = await dbContext.Plants
    .Select(p => new { p.PlantCd, p.Description })
    .ExecuteAsync();

// Combine with Where (filtering happens at the database level)
var activePlants = await dbContext.Plants
    .Select(p => new { p.PlantCd, p.Description })
    .Where(p => p.IsAcive)
    .ExecuteAsync();

// Combine with OrderBy
var orderedPlants = await dbContext.Plants
    .Select(p => new { p.PlantCd, p.Description })
    .OrderBy(p => p.Description)
    .ExecuteAsync();

// Combine with Paginate
var pagedPlants = await dbContext.Plants
    .Select(p => new { p.PlantCd, p.Description })
    .Paginate(0, 10)
    .ExecuteAsync();
```

The Select method modifies the SQL SELECT statement to only include the specified columns, which is more efficient than retrieving all columns. It also supports filtering on properties that are not included in the Select projection, with the filtering happening at the database level.

## Logging
If you want to log the queries, you need to put miminum log level to "Information" and use serilog.

## JOIN
 
Joins can be done with the include tag  (only one to one mapping currently one to many is for future features )
```C#
   var measurements = await dbContext.Measurements
          .Include(x => x.Test)
          .Include(x => x.Plant)
          // then you can also filter only on the parent
          .Where(x => x.Value > 100)
          .OrderBy(x => x.MeasurementDate)
          .Paginate(0, 2)
          .ExecuteAsync();
```

## Interceptors

Interceptors provide a powerful way to hook into the entity lifecycle, allowing you to execute custom logic before or after insert and update operations. This feature gives you complete control over what happens at these critical points.

### Basic Usage

```C#
// Create a DbContext with interceptors
public class ApplicationDbContext : IDisposable
{
    private readonly NpgsqlConnection _connection;
    private IDbTransaction _transaction;

    public DapperSet<TestLalala> Tests { get; }

    public ApplicationDbContext(string connectionString)
    {
        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();
        _transaction = _connection.BeginTransaction();
        
        // Create your custom interceptor
        var yourInterceptor = new YourCustomInterceptor();
        
        // Use the interceptor with a DapperSet
        Tests = new DapperSet<TestLalala>(_connection, _transaction, yourInterceptor);
    }

    // ... Commit, Rollback, Dispose methods
}
```

### Creating Interceptors

You can create interceptors by implementing `ISaveChangesInterceptor` or extending the `SaveChangesInterceptor` base class. This gives you complete freedom to decide what happens before or after insert/update operations:

```C#
public class YourCustomInterceptor : SaveChangesInterceptor
{
    // Called before an entity is inserted
    public override Task BeforeInsertAsync(object entity, CancellationToken cancellationToken = default)
    {
        // Your custom logic here
        if (entity is TestLalala test)
        {
            // For example, set creation date
            test.CreatedDate = DateTime.UtcNow;
        }
        
        return Task.CompletedTask;
    }
    
    // Called after an entity is inserted
    public override Task AfterInsertAsync(object entity, CancellationToken cancellationToken = default)
    {
        // Post-insert logic here
        return Task.CompletedTask;
    }
    
    // Called before an entity is updated
    public override Task BeforeUpdateAsync(object entity, CancellationToken cancellationToken = default)
    {
        // Your custom update logic here
        if (entity is TestLalala test)
        {
            // For example, set last edit date
            test.LastEditDate = DateTime.UtcNow;
        }
        
        return Task.CompletedTask;
    }
    
    // Called after an entity is updated
    public override Task AfterUpdateAsync(object entity, CancellationToken cancellationToken = default)
    {
        // Post-update logic here
        return Task.CompletedTask;
    }
}
```

### Example: Audit Interceptor

Here's an example of an interceptor that handles both creation and modification dates:

```C#
public class AuditInterceptor : SaveChangesInterceptor
{
    public override Task BeforeInsertAsync(object entity, CancellationToken cancellationToken = default)
    {
        if (entity is TestLalala test)
        {
            // Set creation date on insert
            test.CreatedDate = DateTime.UtcNow;
            // Initialize last edit date to same value
            test.LastEditDate = test.CreatedDate;
        }
        
        return Task.CompletedTask;
    }
    
    public override Task BeforeUpdateAsync(object entity, CancellationToken cancellationToken = default)
    {
        if (entity is TestLalala test)
        {
            // Only update the last edit date, not the creation date
            test.LastEditDate = DateTime.UtcNow;
        }
        
        return Task.CompletedTask;
    }
}
```

### Multiple Interceptors

You can use multiple interceptors together, and they will be executed in the order they are provided:

```C#
// Create multiple interceptors
var auditInterceptor = new AuditInterceptor();
var validationInterceptor = new ValidationInterceptor();
var loggingInterceptor = new LoggingInterceptor();

// Use them together
var entitySet = new DapperSet<TestLalala>(
    _connection,
    _transaction,
    auditInterceptor,  // Executed first
    validationInterceptor,  // Executed second
    loggingInterceptor  // Executed third
);
```

### Example: Validation Interceptor

```C#
public class ValidationInterceptor : SaveChangesInterceptor
{
    public override Task BeforeInsertAsync(object entity, CancellationToken cancellationToken = default)
    {
        ValidateEntity(entity);
        return Task.CompletedTask;
    }
    
    public override Task BeforeUpdateAsync(object entity, CancellationToken cancellationToken = default)
    {
        ValidateEntity(entity);
        return Task.CompletedTask;
    }
    
    private void ValidateEntity(object entity)
    {
        if (entity is TestLalala test)
        {
            // Perform validation
            if (string.IsNullOrEmpty(test.Description))
            {
                throw new ArgumentException("Description cannot be empty");
            }
            
            if (test.Precision < 0 || test.Precision > 100)
            {
                throw new ArgumentException("Precision must be between 0 and 100");
            }
        }
    }
}
```

### Example: Logging Interceptor

```C#
public class LoggingInterceptor : SaveChangesInterceptor
{
    private readonly ILogger _logger;
    
    public LoggingInterceptor(ILogger logger)
    {
        _logger = logger;
    }
    
    public override Task BeforeInsertAsync(object entity, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Inserting entity of type {entity.GetType().Name}");
        return Task.CompletedTask;
    }
    
    public override Task AfterInsertAsync(object entity, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Successfully inserted entity of type {entity.GetType().Name}");
        return Task.CompletedTask;
    }
    
    public override Task BeforeUpdateAsync(object entity, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Updating entity of type {entity.GetType().Name}");
        return Task.CompletedTask;
    }
    
    public override Task AfterUpdateAsync(object entity, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Successfully updated entity of type {entity.GetType().Name}");
        return Task.CompletedTask;
    }
}
```

### Example: User Tracking Interceptor

This advanced interceptor tracks which user created or last modified an entity, providing accountability in multi-user systems:

```C#
public class UserTrackingInterceptor : SaveChangesInterceptor
{
    // Current user context
    public string CurrentUser { get; set; }
    public string CurrentRole { get; set; }

    public UserTrackingInterceptor(string initialUser, string initialRole)
    {
        CurrentUser = initialUser;
        CurrentRole = initialRole;
    }

    public override Task BeforeInsertAsync(object entity, CancellationToken cancellationToken = default)
    {
        if (entity is IUserTrackable trackable)
        {
            // Set both created and last edited fields on insert
            trackable.CreatedBy = CurrentUser;
            trackable.CreatedRole = CurrentRole;
            trackable.LastEditedBy = CurrentUser;
            trackable.LastEditedRole = CurrentRole;
        }
        
        return Task.CompletedTask;
    }
    
    public override Task BeforeUpdateAsync(object entity, CancellationToken cancellationToken = default)
    {
        if (entity is IUserTrackable trackable)
        {
            // Only update the last edited fields, preserve creation info
            trackable.LastEditedBy = CurrentUser;
            trackable.LastEditedRole = CurrentRole;
        }
        
        return Task.CompletedTask;
    }
}

// Interface for trackable entities
public interface IUserTrackable
{
    string CreatedBy { get; set; }
    string CreatedRole { get; set; }
    string LastEditedBy { get; set; }
    string LastEditedRole { get; set; }
}

// Example entity implementing the interface
public class UserTrackableEntity : IUserTrackable
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; }
    
    [Column("created_by")]
    public string CreatedBy { get; set; }
    
    [Column("created_role")]
    public string CreatedRole { get; set; }
    
    [Column("last_edited_by")]
    public string LastEditedBy { get; set; }
    
    [Column("last_edited_role")]
    public string LastEditedRole { get; set; }
}
```

### Example: Versioning Interceptor

This interceptor automatically manages version numbers for entities, which is useful for optimistic concurrency and tracking changes:

```C#
public class VersioningInterceptor : SaveChangesInterceptor
{
    public override Task BeforeInsertAsync(object entity, CancellationToken cancellationToken = default)
    {
        if (entity is IVersionable versionable)
        {
            // Start at version 1 for new entities
            versionable.Version = 1;
        }
        
        return Task.CompletedTask;
    }
    
    public override Task BeforeUpdateAsync(object entity, CancellationToken cancellationToken = default)
    {
        if (entity is IVersionable versionable)
        {
            // Increment version on each update
            versionable.Version++;
        }
        
        return Task.CompletedTask;
    }
}

// Interface for versionable entities
public interface IVersionable
{
    int Version { get; set; }
}

// Example entity implementing the interface
public class VersionedEntity : IVersionable
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; }
    
    [Column("version")]
    public int Version { get; set; }
}
```

### Example: Combining Multiple Interceptors

This example shows how to combine multiple interceptors to create a comprehensive entity lifecycle management system:

```C#
// Create a DbContext with multiple interceptors
public class ApplicationDbContext : IDisposable
{
    private readonly NpgsqlConnection _connection;
    private IDbTransaction _transaction;

    public DapperSet<ComplexEntity> ComplexEntities { get; }

    public ApplicationDbContext(string connectionString, string currentUser, string currentRole)
    {
        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();
        _transaction = _connection.BeginTransaction();
        
        // Create multiple interceptors
        var auditInterceptor = new AuditInterceptor();
        var userTrackingInterceptor = new UserTrackingInterceptor(currentUser, currentRole);
        var versioningInterceptor = new VersioningInterceptor();
        var validationInterceptor = new ValidationInterceptor();
        
        // Use them together with a DapperSet
        ComplexEntities = new DapperSet<ComplexEntity>(
            _connection,
            _transaction,
            auditInterceptor,        // Handles creation and modification dates
            userTrackingInterceptor, // Tracks who created/modified the entity
            versioningInterceptor,   // Manages version numbers
            validationInterceptor    // Validates the entity before saving
        );
    }

    // ... Commit, Rollback, Dispose methods
}

// A complex entity that implements multiple interfaces
public class ComplexEntity : IUserTrackable, IVersionable
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; }
    
    [Column("created_date")]
    public DateTime CreatedDate { get; set; }
    
    [Column("last_edit_date")]
    public DateTime LastEditDate { get; set; }
    
    [Column("created_by")]
    public string CreatedBy { get; set; }
    
    [Column("created_role")]
    public string CreatedRole { get; set; }
    
    [Column("last_edited_by")]
    public string LastEditedBy { get; set; }
    
    [Column("last_edited_role")]
    public string LastEditedRole { get; set; }
    
    [Column("version")]
    public int Version { get; set; }
}
```

This approach allows you to build a modular, maintainable system where each interceptor handles a specific concern, following the Single Responsibility Principle.

Classes need to be annotated like this with the NotMapped and ForeignKey tag
```C#
 [Table("ipc.measurement")]
public class CoolMeasurement
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("test_cd")]
    public required string TestCd { get; set; }

    [Column("plant_cd")]
    public required string PlantCd { get; set; }

    [Column("avg_value")]
    public double Value { get; set; }

    [Column("measurement_date")]
    public DateTime MeasurementDate { get; set; } = DateTime.UtcNow;

    [NotMapped]
    [ForeignKey("test_cd")]
    public TestLalala Test { get; set; }

    [NotMapped]
    [ForeignKey("plant_cd")]
    public Plant Plant { get; set; }
}

```
