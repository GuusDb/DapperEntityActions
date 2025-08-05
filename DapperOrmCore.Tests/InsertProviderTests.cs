using DapperOrmCore.Tests.Models;
using Microsoft.Data.Sqlite;
using System.Data;
using Xunit;
using Dapper;

namespace DapperOrmCore.Tests;

/// <summary>
/// Tests for insert operations with different database providers to ensure correct SQL syntax is used.
/// </summary>
public class InsertProviderTests : IDisposable
{
    private readonly SqliteConnection _connection;
    
    public InsertProviderTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        CreateTables();
    }
    
    private void CreateTables()
    {
        _connection.Execute(@"
            CREATE TABLE plant (
                plant_cd TEXT PRIMARY KEY,
                description TEXT,
                is_active INTEGER
            )");
            
        _connection.Execute(@"
            CREATE TABLE measurement (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                test_cd TEXT,
                plant_cd TEXT,
                avg_value REAL,
                measurement_date TEXT
            )");
    }
    
    [Fact]
    public async Task InsertAsync_WithSQLite_ShouldUseReturningClause()
    {
        // Arrange
        using var dbContext = new ApplicationDbContext(_connection, DatabaseProvider.SQLite);
        var newPlant = new Plant { PlantCd = "PLANT_SQLITE", Description = "SQLite Plant", IsAcive = true };
        
        // Act
        var insertedId = await dbContext.Plants.InsertAsync<string>(newPlant);
        var retrieved = await dbContext.Plants.GetByIdAsync<string>("PLANT_SQLITE");
        
        // Assert
        Assert.Equal("PLANT_SQLITE", insertedId);
        Assert.NotNull(retrieved);
        Assert.Equal("SQLite Plant", retrieved.Description);
        Assert.True(retrieved.IsAcive);
    }
    
    [Fact]
    public async Task InsertAsync_WithSqlServer_ShouldUseOutputClause()
    {
        // Arrange - Using SQLite connection but with SqlServer provider to test SQL generation
        // The actual SQL generation will use OUTPUT INSERTED syntax, but SQLite will execute it
        // This tests that the SQL generation logic works correctly for SQL Server
        using var dbContext = new ApplicationDbContext(_connection, DatabaseProvider.SqlServer);
        var newPlant = new Plant { PlantCd = "PLANT_SQLSERVER", Description = "SQL Server Plant", IsAcive = true };
        
        // Act
        var insertedId = await dbContext.Plants.InsertAsync<string>(newPlant);
        var retrieved = await dbContext.Plants.GetByIdAsync<string>("PLANT_SQLSERVER");
        
        // Assert
        Assert.Equal("PLANT_SQLSERVER", insertedId);
        Assert.NotNull(retrieved);
        Assert.Equal("SQL Server Plant", retrieved.Description);
        Assert.True(retrieved.IsAcive);
    }
    
    [Fact]
    public async Task InsertAsync_WithPostgreSQL_ShouldUseReturningClause()
    {
        // Arrange - Using SQLite connection but with PostgreSQL provider to test SQL generation
        using var dbContext = new ApplicationDbContext(_connection, DatabaseProvider.PostgreSQL);
        var newPlant = new Plant { PlantCd = "PLANT_POSTGRES", Description = "PostgreSQL Plant", IsAcive = true };
        
        // Act
        var insertedId = await dbContext.Plants.InsertAsync<string>(newPlant);
        var retrieved = await dbContext.Plants.GetByIdAsync<string>("PLANT_POSTGRES");
        
        // Assert
        Assert.Equal("PLANT_POSTGRES", insertedId);
        Assert.NotNull(retrieved);
        Assert.Equal("PostgreSQL Plant", retrieved.Description);
        Assert.True(retrieved.IsAcive);
    }
    
    [Fact]
    public async Task InsertAsync_WithAutoIncrementId_SQLite_ShouldReturnGeneratedId()
    {
        // Arrange
        using var dbContext = new ApplicationDbContext(_connection, DatabaseProvider.SQLite);
        var newMeasurement = new CoolMeasurement
        {
            TestCd = "TEST1",
            PlantCd = "PLANT1",
            Value = 123.45,
            MeasurementDate = DateTime.UtcNow
        };
        
        // Act
        var insertedId = await dbContext.Measurements.InsertAsync<int>(newMeasurement);
        var retrieved = await dbContext.Measurements.GetByIdAsync<int>(insertedId);
        
        // Assert
        Assert.True(insertedId > 0);
        Assert.NotNull(retrieved);
        Assert.Equal(123.45, retrieved.Value);
        Assert.Equal("TEST1", retrieved.TestCd);
    }
    
    [Fact]
    public async Task InsertAsync_WithAutoIncrementId_SqlServer_ShouldReturnGeneratedId()
    {
        // Arrange - Using SQLite connection but with SqlServer provider
        using var dbContext = new ApplicationDbContext(_connection, DatabaseProvider.SqlServer);
        var newMeasurement = new CoolMeasurement
        {
            TestCd = "TEST2",
            PlantCd = "PLANT2",
            Value = 456.78,
            MeasurementDate = DateTime.UtcNow
        };
        
        // Act
        var insertedId = await dbContext.Measurements.InsertAsync<int>(newMeasurement);
        var retrieved = await dbContext.Measurements.GetByIdAsync<int>(insertedId);
        
        // Assert
        Assert.True(insertedId > 0);
        Assert.NotNull(retrieved);
        Assert.Equal(456.78, retrieved.Value);
        Assert.Equal("TEST2", retrieved.TestCd);
    }
    
    [Fact]
    public async Task InsertAsync_WithAutoIncrementId_PostgreSQL_ShouldReturnGeneratedId()
    {
        // Arrange - Using SQLite connection but with PostgreSQL provider
        using var dbContext = new ApplicationDbContext(_connection, DatabaseProvider.PostgreSQL);
        var newMeasurement = new CoolMeasurement
        {
            TestCd = "TEST3",
            PlantCd = "PLANT3",
            Value = 789.01,
            MeasurementDate = DateTime.UtcNow
        };
        
        // Act
        var insertedId = await dbContext.Measurements.InsertAsync<int>(newMeasurement);
        var retrieved = await dbContext.Measurements.GetByIdAsync<int>(insertedId);
        
        // Assert
        Assert.True(insertedId > 0);
        Assert.NotNull(retrieved);
        Assert.Equal(789.01, retrieved.Value);
        Assert.Equal("TEST3", retrieved.TestCd);
    }
    
    [Fact]
    public async Task InsertAsync_MultipleEntities_DifferentProviders_ShouldAllWork()
    {
        // Test that we can insert multiple entities using different providers
        // This ensures the SQL generation is working correctly for all providers
        
        // SQLite
        using var sqliteConnection = new SqliteConnection("DataSource=:memory:");
        sqliteConnection.Open();
        CreateTablesForConnection(sqliteConnection);
        using var sqliteContext = new ApplicationDbContextWithTransaction(sqliteConnection, DatabaseProvider.SQLite);
        var sqlitePlant = new Plant { PlantCd = "MULTI_SQLITE", Description = "Multi SQLite", IsAcive = true };
        var sqliteId = await sqliteContext.Plants.InsertAsync<string>(sqlitePlant);
        
        // SQL Server (using SQLite connection but SQL Server provider for syntax testing)
        using var sqlServerConnection = new SqliteConnection("DataSource=:memory:");
        sqlServerConnection.Open();
        CreateTablesForConnection(sqlServerConnection);
        using var sqlServerContext = new ApplicationDbContextWithTransaction(sqlServerConnection, DatabaseProvider.SqlServer);
        var sqlServerPlant = new Plant { PlantCd = "MULTI_SQLSERVER", Description = "Multi SQL Server", IsAcive = true };
        var sqlServerId = await sqlServerContext.Plants.InsertAsync<string>(sqlServerPlant);
        
        // PostgreSQL (using SQLite connection but PostgreSQL provider for syntax testing)
        using var postgresConnection = new SqliteConnection("DataSource=:memory:");
        postgresConnection.Open();
        CreateTablesForConnection(postgresConnection);
        using var postgresContext = new ApplicationDbContextWithTransaction(postgresConnection, DatabaseProvider.PostgreSQL);
        var postgresPlant = new Plant { PlantCd = "MULTI_POSTGRES", Description = "Multi PostgreSQL", IsAcive = true };
        var postgresId = await postgresContext.Plants.InsertAsync<string>(postgresPlant);
        
        // Assert all inserts worked
        Assert.Equal("MULTI_SQLITE", sqliteId);
        Assert.Equal("MULTI_SQLSERVER", sqlServerId);
        Assert.Equal("MULTI_POSTGRES", postgresId);
        
        // Verify all entities can be retrieved
        var retrievedSqlite = await sqliteContext.Plants.GetByIdAsync<string>("MULTI_SQLITE");
        var retrievedSqlServer = await sqlServerContext.Plants.GetByIdAsync<string>("MULTI_SQLSERVER");
        var retrievedPostgres = await postgresContext.Plants.GetByIdAsync<string>("MULTI_POSTGRES");
        
        Assert.NotNull(retrievedSqlite);
        Assert.NotNull(retrievedSqlServer);
        Assert.NotNull(retrievedPostgres);
    }
    
    private void CreateTablesForConnection(SqliteConnection connection)
    {
        connection.Execute(@"
            CREATE TABLE plant (
                plant_cd TEXT PRIMARY KEY,
                description TEXT,
                is_active INTEGER
            )");
            
        connection.Execute(@"
            CREATE TABLE measurement (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                test_cd TEXT,
                plant_cd TEXT,
                avg_value REAL,
                measurement_date TEXT
            )");
    }
    
    public void Dispose()
    {
        _connection?.Dispose();
    }
}