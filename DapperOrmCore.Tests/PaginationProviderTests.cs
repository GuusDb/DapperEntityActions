using DapperOrmCore.Tests.Models;
using Microsoft.Data.Sqlite;
using System.Data;
using Xunit;
using Dapper;

namespace DapperOrmCore.Tests;

public class PaginationProviderTests : IDisposable
{
    private readonly SqliteConnection _connection;
    
    public PaginationProviderTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        CreateTables();
        SeedData();
    }
    
    private void CreateTables()
    {
        _connection.Execute(@"
            CREATE TABLE plant (
                plant_cd TEXT PRIMARY KEY,
                description TEXT,
                is_active INTEGER
            )");
    }
    
    private void SeedData()
    {
        // Clear any existing data
        _connection.Execute("DELETE FROM plant");
        
        // Insert 20 plants for pagination testing in a specific order
        for (int i = 1; i <= 20; i++)
        {
            _connection.Execute(@"
                INSERT INTO plant (plant_cd, description, is_active)
                VALUES (@PlantCd, @Description, @IsActive)",
                new { PlantCd = $"PLANT{i:D2}", Description = $"Plant {i}", IsActive = i % 2 == 0 ? 1 : 0 });
        }
    }
    
    [Fact]
    public async Task Paginate_WithSQLite_ShouldUseCorrectSyntax()
    {
        // Arrange
        using var dbContext = new ApplicationDbContext(_connection, DatabaseProvider.SQLite);
        
        // Act
        var page1 = await dbContext.Plants
            .OrderBy(p => p.PlantCd)
            .Paginate(0, 5)
            .ExecuteAsync();
            
        var page2 = await dbContext.Plants
            .OrderBy(p => p.PlantCd)
            .Paginate(1, 5)
            .ExecuteAsync();
        
        // Assert
        var page1Plants = page1.ToList();
        var page2Plants = page2.ToList();
        
        Assert.Equal(5, page1Plants.Count);
        Assert.Equal(5, page2Plants.Count);
        
        // First page should have PLANT01 through PLANT05
        Assert.Equal("PLANT01", page1Plants[0].PlantCd);
        Assert.Equal("PLANT05", page1Plants[4].PlantCd);
        
        // Second page should have PLANT06 through PLANT10
        Assert.Equal("PLANT06", page2Plants[0].PlantCd);
        Assert.Equal("PLANT10", page2Plants[4].PlantCd);
    }
    
    [Fact]
    public async Task Paginate_WithSqlServer_ShouldUseCorrectSyntax()
    {
        // Arrange - Using SQLite but with SqlServer provider to test the SQL generation
        using var dbContext = new ApplicationDbContext(_connection, DatabaseProvider.SqlServer);
        
        // Act
        var page1 = await dbContext.Plants
            .OrderBy(p => p.PlantCd)
            .Paginate(0, 5)
            .ExecuteAsync();
            
        var page2 = await dbContext.Plants
            .OrderBy(p => p.PlantCd)
            .Paginate(1, 5)
            .ExecuteAsync();
        
        // Assert
        var page1Plants = page1.ToList();
        var page2Plants = page2.ToList();
        
        Assert.Equal(5, page1Plants.Count);
        Assert.Equal(5, page2Plants.Count);
        
        // First page should have PLANT01 through PLANT05
        Assert.Equal("PLANT01", page1Plants[0].PlantCd);
        Assert.Equal("PLANT05", page1Plants[4].PlantCd);
        
        // Second page should have PLANT06 through PLANT10
        Assert.Equal("PLANT06", page2Plants[0].PlantCd);
        Assert.Equal("PLANT10", page2Plants[4].PlantCd);
    }
    
    [Fact]
    public async Task Paginate_WithPostgreSQL_ShouldUseCorrectSyntax()
    {
        // Arrange - Using SQLite but with PostgreSQL provider to test the SQL generation
        using var dbContext = new ApplicationDbContext(_connection, DatabaseProvider.PostgreSQL);
        
        // Act
        var page1 = await dbContext.Plants
            .OrderBy(p => p.PlantCd)
            .Paginate(0, 5)
            .ExecuteAsync();
            
        var page2 = await dbContext.Plants
            .OrderBy(p => p.PlantCd)
            .Paginate(1, 5)
            .ExecuteAsync();
        
        // Assert
        var page1Plants = page1.ToList();
        var page2Plants = page2.ToList();
        
        Assert.Equal(5, page1Plants.Count);
        Assert.Equal(5, page2Plants.Count);
        
        // First page should have PLANT01 through PLANT05
        Assert.Equal("PLANT01", page1Plants[0].PlantCd);
        Assert.Equal("PLANT05", page1Plants[4].PlantCd);
        
        // Second page should have PLANT06 through PLANT10
        Assert.Equal("PLANT06", page2Plants[0].PlantCd);
        Assert.Equal("PLANT10", page2Plants[4].PlantCd);
    }
    
    [Fact]
    public async Task Paginate_WithoutOrderBy_SqlServer_ShouldAddDefaultOrderBy()
    {
        // Arrange - Using SQLite but with SqlServer provider to test the SQL generation
        using var dbContext = new ApplicationDbContext(_connection, DatabaseProvider.SqlServer);
        
        // Act - Note: No OrderBy clause
        var page1 = await dbContext.Plants
            .Paginate(0, 5)
            .ExecuteAsync();
        
        // Assert
        Assert.Equal(5, page1.Count());
    }
    
    public void Dispose()
    {
        _connection?.Dispose();
    }
}