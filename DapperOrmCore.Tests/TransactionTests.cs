using DapperOrmCore.Tests.Models;
using Microsoft.Data.Sqlite;
using System.Data;
using Xunit;
using Dapper;

namespace DapperOrmCore.Tests;

public class TransactionTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContextWithTransaction _dbContext;

    public TransactionTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _dbContext = new ApplicationDbContextWithTransaction(_connection);
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

        _connection.Execute(@"
            CREATE TABLE test (
                test_cd TEXT PRIMARY KEY,
                description TEXT,
                is_active INTEGER,
                test_type_cd TEXT,
                test_mode_cd TEXT,
                precision INTEGER,
                created_date TEXT,
                last_edit_date TEXT
            )");

        _connection.Execute(@"
            CREATE TABLE measurement (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                test_cd TEXT,
                plant_cd TEXT,
                avg_value REAL,
                measurement_date TEXT,
                FOREIGN KEY (test_cd) REFERENCES test(test_cd),
                FOREIGN KEY (plant_cd) REFERENCES plant(plant_cd)
            )");
    }

    private void SeedData()
    {
        _connection.Execute(@"
            INSERT INTO plant (plant_cd, description, is_active)
            VALUES 
                ('PLANT1', 'Plant 1', 1),
                ('PLANT2', 'Plant 2', 1),
                ('PLANT3', 'Plant 3', 0)");

        _connection.Execute(@"
            INSERT INTO test (test_cd, description, is_active, test_type_cd, test_mode_cd, precision, created_date, last_edit_date)
            VALUES 
                ('TEST1', 'Test 1', 1, 'Dimensional', 'InProcess', 80, '2025-04-20T00:00:00Z', '2025-04-20T00:00:00Z'),
                ('TEST2', 'Test 2', 1, 'Dimensional', 'Offline', 85, '2025-04-21T00:00:00Z', '2025-04-21T00:00:00Z')");

        _connection.Execute(@"
            INSERT INTO measurement (test_cd, plant_cd, avg_value, measurement_date)
            VALUES 
                ('TEST1', 'PLANT1', 150.5, '2025-04-22T10:00:00Z'),
                ('TEST1', 'PLANT2', 200.0, '2025-04-22T11:00:00Z'),
                ('TEST2', 'PLANT1', 50.0, '2025-04-22T12:00:00Z')");
    }

    [Fact]
    public async Task ExecuteAsync_WithTransaction_ShouldReturnResults()
    {
        // Arrange
        using var transaction = _dbContext.BeginTransaction();

        // Act
        var results = await _dbContext.Plants.ExecuteAsync(transaction);

        // Assert
        Assert.Equal(3, results.Count());
    }

    [Fact]
    public async Task InsertAsync_WithTransaction_ShouldInsertEntityWithinTransaction()
    {
        // Arrange
        var newPlant = new Plant { PlantCd = "PLANT5", Description = "Plant 5", IsAcive = true };
        using var transaction = _dbContext.BeginTransaction();

        // Act
        var insertedId = await _dbContext.Plants.InsertAsync<string>(newPlant, transaction);
        
        // Verify the entity exists within the transaction
        var retrievedWithinTx = await _dbContext.Plants.GetByIdAsync<string>("PLANT5");
        Assert.NotNull(retrievedWithinTx);
        
        // Rollback the transaction
        _dbContext.Rollback();
        
        // Verify the entity doesn't exist after rollback
        var retrievedAfterRollback = await _dbContext.Plants.GetByIdAsync<string>("PLANT5");
        
        // Assert
        Assert.Equal("PLANT5", insertedId);
        Assert.Null(retrievedAfterRollback);
    }

    [Fact]
    public async Task UpdateAsync_WithTransaction_ShouldUpdateEntityWithinTransaction()
    {
        // Arrange
        var plant = await _dbContext.Plants.GetByIdAsync<string>("PLANT2");
        plant.Description = "Updated Plant 2";
        plant.IsAcive = false;
        using var transaction = _dbContext.BeginTransaction();

        // Act
        var result = await _dbContext.Plants.UpdateAsync(plant, transaction);
        
        // Verify the entity is updated within the transaction
        var updatedWithinTx = await _dbContext.Plants.GetByIdAsync<string>("PLANT2");
        Assert.Equal("Updated Plant 2", updatedWithinTx.Description);
        
        // Rollback the transaction
        _dbContext.Rollback();
        
        // Verify the entity is not updated after rollback
        var plantAfterRollback = await _dbContext.Plants.GetByIdAsync<string>("PLANT2");
        
        // Assert
        Assert.True(result);
        Assert.Equal("Plant 2", plantAfterRollback.Description);
        Assert.True(plantAfterRollback.IsAcive);
    }

    [Fact]
    public async Task DeleteAsync_WithTransaction_ShouldDeleteEntityWithinTransaction()
    {
        // Arrange
        using var transaction = _dbContext.BeginTransaction();

        // First, delete any measurements that reference PLANT2 to avoid foreign key constraint violations
        _connection.Execute("DELETE FROM measurement WHERE plant_cd = 'PLANT2'", transaction: transaction);

        // Act
        var result = await _dbContext.Plants.DeleteAsync<string>("PLANT2", transaction);
        
        // Verify the entity is deleted within the transaction
        var deletedWithinTx = await _dbContext.Plants.GetByIdAsync<string>("PLANT2");
        Assert.Null(deletedWithinTx);
        
        // Rollback the transaction
        _dbContext.Rollback();
        
        // Verify the entity still exists after rollback
        var plantAfterRollback = await _dbContext.Plants.GetByIdAsync<string>("PLANT2");
        
        // Assert
        Assert.True(result);
        Assert.NotNull(plantAfterRollback);
        Assert.Equal("Plant 2", plantAfterRollback.Description);
    }

    [Fact]
    public async Task GetByIdAsync_WithTransaction_ShouldReturnEntityWithinTransaction()
    {
        // Arrange
        using var transaction = _dbContext.BeginTransaction();

        // Act
        var plant = await _dbContext.Plants.GetByIdAsync<string>("PLANT1", transaction);
        
        // Assert
        Assert.NotNull(plant);
        Assert.Equal("Plant 1", plant.Description);
        Assert.True(plant.IsAcive);
    }

    [Fact]
    public async Task GetByIdAsync_WithTransaction_ShouldRespectTransactionRollback()
    {
        // Arrange
        var newPlant = new Plant { PlantCd = "PLANT7", Description = "Plant 7", IsAcive = true };
        using var transaction = _dbContext.BeginTransaction();
        
        // Insert a new plant within the transaction
        await _dbContext.Plants.InsertAsync<string>(newPlant, transaction);
        
        // Act - This should find the plant within the same transaction
        var plantWithinTx = await _dbContext.Plants.GetByIdAsync<string>("PLANT7", transaction);
        
        // Assert the plant exists within the transaction
        Assert.NotNull(plantWithinTx);
        Assert.Equal("Plant 7", plantWithinTx.Description);
        
        // Rollback the transaction
        _dbContext.Rollback();
        
        // After rollback, the plant should not exist
        var plantAfterRollback = await _dbContext.Plants.GetByIdAsync<string>("PLANT7");
        Assert.Null(plantAfterRollback); // Should not exist after rollback
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _connection?.Dispose();
    }
}