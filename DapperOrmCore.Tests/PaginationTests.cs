using DapperOrmCore.Tests.Models;
namespace DapperOrmCore.Tests;

public class PaginationTests : TestSetup
{
    [Fact]
    public async Task Paginate_FirstPage_ShouldReturnCorrectMeasurements()
    {
        // Act
        var results = await DbContext.Measurements
            .Paginate(0, 2)
            .ExecuteAsync();

        // Assert
        Assert.Equal(2, results.Count());
        Assert.Equal(150.5, results.First().Value);
    }

    [Fact]
    public async Task Paginate_SecondPage_ShouldReturnCorrectMeasurements()
    {
        // Act
        var results = await DbContext.Measurements
            .Paginate(1, 2)
            .ExecuteAsync();

        // Assert
        Assert.Equal(2, results.Count());
        Assert.Equal(50.0, results.First().Value);
    }

    [Fact]
    public async Task Paginate_BeyondData_ShouldReturnEmpty()
    {
        // Act
        var results = await DbContext.Measurements
            .Paginate(10, 2)
            .ExecuteAsync();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task Paginate_NegativePageIndex_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => DbContext.Measurements
            .Paginate(-1, 2)
            .ExecuteAsync());
    }

    [Fact]
    public async Task Paginate_ZeroPageSize_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => DbContext.Measurements
            .Paginate(0, 0)
            .ExecuteAsync());
    }
}