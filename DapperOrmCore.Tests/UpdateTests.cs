using DapperOrmCore.Tests.Models;

namespace DapperOrmCore.Tests;

public class UpdateTests : TestSetup
{
    [Fact]
    public async Task UpdateAsync_Plant_ShouldUpdateSuccessfully()
    {
        // Arrange
        var plantToUpdate = new Plant { PlantCd = "PLANT1", Description = "Updated Plant", IsAcive = false };

        // Act
        var result = await DbContext.Plants.UpdateAsync(plantToUpdate);
        var updated = await DbContext.Plants.GetByIdAsync<string>("PLANT1");

        // Assert
        Assert.True(result);
        Assert.NotNull(updated);
        Assert.Equal("Updated Plant", updated.Description);
        Assert.False(updated.IsAcive);
    }

    [Fact]
    public async Task UpdateAsync_Test_ShouldUpdateSuccessfully()
    {
        // Arrange
        var testToUpdate = new TestLalala
        {
            TestCd = "TEST1",
            Description = "Updated Test",
            IsActive = false,
            TestType = "Functional",
            TestMode = "Offline",
            Precision = 90,
            CreatedDate = DateTime.UtcNow,
            LastEditDate = DateTime.UtcNow
        };

        // Act
        var result = await DbContext.Tests.UpdateAsync(testToUpdate);
        var updated = await DbContext.Tests.GetByIdAsync<string>("TEST1");

        // Assert
        Assert.True(result);
        Assert.NotNull(updated);
        Assert.Equal("Updated Test", updated.Description);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_Measurement_ShouldUpdateSuccessfully()
    {
        // Arrange
        var measurementToUpdate = new CoolMeasurement
        {
            Id = 1,
            TestCd = "TEST1",
            PlantCd = "PLANT1",
            Value = 999.9,
            MeasurementDate = DateTime.UtcNow
        };

        // Act
        var result = await DbContext.Measurements.UpdateAsync(measurementToUpdate);
        var updated = await DbContext.Measurements.GetByIdAsync<int>(1);

        // Assert
        Assert.True(result);
        Assert.NotNull(updated);
        Assert.Equal(999.9, updated.Value);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentEntity_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var nonExistentPlant = new Plant { PlantCd = "INVALID", Description = "Test", IsAcive = true };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => DbContext.Plants.UpdateAsync(nonExistentPlant));
    }

    [Fact]
    public async Task UpdateAsync_NullEntity_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => DbContext.Tests.UpdateAsync(null));
    }
}