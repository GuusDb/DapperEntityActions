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
## Pagination
```C#
  var specificTests = await dbContext.Tests
           // Other linq queries
            .Paginate(0,50) // paginate(pageIndex, pageSize), this gives records 0-50 
            .ExecuteAsync();
```

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
