using DapperOrmCore.Tests.Models;
using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Data.Sqlite;

namespace DapperOrmCore.Tests;

public class InsertTests : TestSetup
{
    [Fact]
    public async Task InsertAsync_Plant_ShouldInsertSuccessfully()
    {
        // Arrange
        var newPlant = new Plant { PlantCd = "PLANT5", Description = "New Plant", IsAcive = true };

        // Act
        var insertedId = await DbContext.Plants.InsertAsync<string>(newPlant);
        var retrieved = await DbContext.Plants.GetByIdAsync<string>("PLANT5");

        // Assert
        Assert.Equal("PLANT5", insertedId);
        Assert.NotNull(retrieved);
        Assert.Equal("New Plant", retrieved.Description);
        Assert.True(retrieved.IsAcive);
    }

    [Fact]
    public async Task InsertAsync_Test_ShouldInsertSuccessfully()
    {
        // Arrange
        var newTest = new TestLalala
        {
            TestCd = "TEST5",
            Description = "New Test",
            IsActive = true,
            TestType = "Dimensional",
            TestMode = "InProcess",
            Precision = 85,
            CreatedDate = DateTime.UtcNow,
            LastEditDate = DateTime.UtcNow
        };

        // Act
        var insertedId = await DbContext.Tests.InsertAsync<string>(newTest);
        var retrieved = await DbContext.Tests.GetByIdAsync<string>("TEST5");

        // Assert
        Assert.Equal("TEST5", insertedId);
        Assert.NotNull(retrieved);
        Assert.Equal("New Test", retrieved.Description);
        Assert.True(retrieved.IsActive);
    }

    [Fact]
    public async Task InsertAsync_Measurement_ShouldInsertSuccessfully()
    {
        // Arrange
        var newMeasurement = new CoolMeasurement
        {
            TestCd = "TEST1",
            PlantCd = "PLANT1",
            Value = 175.5,
            MeasurementDate = DateTime.UtcNow
        };

        // Act
        var insertedId = await DbContext.Measurements.InsertAsync<int>(newMeasurement);
        var retrieved = await DbContext.Measurements.GetByIdAsync<int>(insertedId);

        // Assert
        Assert.True(insertedId > 0);
        Assert.NotNull(retrieved);
        Assert.Equal(175.5, retrieved.Value);
        Assert.Equal("TEST1", retrieved.TestCd);
    }

    [Fact]
    public async Task InsertAsync_NullEntity_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => DbContext.Plants.InsertAsync<string>(null));
    }

    [Fact]
    public async Task InsertAsync_InvalidForeignKey_ShouldThrowException()
    {
        // Arrange
        var invalidMeasurement = new CoolMeasurement
        {
            TestCd = "INVALID_TEST",
            PlantCd = "PLANT1",
            Value = 100.0,
            MeasurementDate = DateTime.UtcNow
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => DbContext.Measurements.InsertAsync<int>(invalidMeasurement));
        Assert.IsType<SqliteException>(exception.InnerException);
        var sqliteException = (SqliteException)exception.InnerException;
        Assert.Equal(19, sqliteException.SqliteErrorCode); // 19 = SQLITE_CONSTRAINT (foreign key violation)
        Assert.Contains("FOREIGN KEY constraint failed", sqliteException.Message);
    }
}