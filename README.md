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
dotnet add package CustomDapperEntityActions --version 1.0.2

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

// Linq like query
var specificTests = await dbContext.Tests.WhereAsync(x =>x.IsAcive );

// GetAll
DbContext.Tests.GetAllAsync

```
