using DapperOrmCore.Tests.Models;
using System.Threading.Tasks;
using Xunit;

namespace DapperOrmCore.Tests;

public class DeleteTests : TestSetup
{
    [Fact]
    public async Task DeleteAsync_Plant_ShouldDeleteSuccessfully()
    {
        // Arrange: Delete dependent measurement records first
        await DbContext.Measurements
            .Where(x => x.PlantCd == "PLANT1")
            .ExecuteAsync()
            .ContinueWith(async task =>
            {
                foreach (var measurement in await task)
                {
                    await DbContext.Measurements.DeleteAsync<int>(measurement.Id);
                }
            });

        // Act
        var result = await DbContext.Plants.DeleteAsync<string>("PLANT1");
        var deleted = await DbContext.Plants.GetByIdAsync<string>("PLANT1");

        // Assert
        Assert.True(result);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_Test_ShouldDeleteSuccessfully()
    {
        // Arrange: Delete dependent measurement records first
        await DbContext.Measurements
            .Where(x => x.TestCd == "TEST1")
            .ExecuteAsync()
            .ContinueWith(async task =>
            {
                foreach (var measurement in await task)
                {
                    await DbContext.Measurements.DeleteAsync<int>(measurement.Id);
                }
            });

        // Act
        var result = await DbContext.Tests.DeleteAsync<string>("TEST1");
        var deleted = await DbContext.Tests.GetByIdAsync<string>("TEST1");

        // Assert
        Assert.True(result);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_Measurement_ShouldDeleteSuccessfully()
    {
        // Act
        var result = await DbContext.Measurements.DeleteAsync<int>(1);
        var deleted = await DbContext.Measurements.GetByIdAsync<int>(1);

        // Assert
        Assert.True(result);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentEntity_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => DbContext.Plants.DeleteAsync<string>("INVALID"));
    }

    [Fact]
    public async Task DeleteAsync_NullId_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => DbContext.Tests.DeleteAsync<string>(null));
    }
}