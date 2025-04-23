using DapperOrmCore.Tests.Models;

namespace DapperOrmCore.Tests;

public class OrderByTests : TestSetup
{
    [Fact]
    public async Task OrderBy_Ascending_ShouldReturnTestsInOrder()
    {
        // Act
        var results = await DbContext.Tests
            .OrderBy(x => x.TestCd)
            .ExecuteAsync();

        // Assert
        var ordered = results.OrderBy(x => x.TestCd).ToList();
        Assert.Equal(ordered, results);
    }

    [Fact]
    public async Task OrderBy_Descending_ShouldReturnTestsInOrder()
    {
        // Act
        var results = await DbContext.Tests
            .OrderBy(x => x.TestCd, descending: true)
            .ExecuteAsync();

        // Assert
        var ordered = results.OrderByDescending(x => x.TestCd).ToList();
        Assert.Equal(ordered, results);
    }

    [Fact]
    public async Task OrderBy_MultipleFields_ShouldReturnMeasurementsInOrder()
    {
        // Act
        var results = await DbContext.Measurements
            .OrderBy(x => x.TestCd)
            .OrderBy(x => x.Value)
            .ExecuteAsync();

        // Assert
        var ordered = results.OrderBy(x => x.TestCd).ThenBy(x => x.Value).ToList();
        Assert.Equal(ordered, results);
    }

    [Fact]
    public async Task OrderBy_DateField_ShouldReturnMeasurementsInOrder()
    {
        // Act
        var results = await DbContext.Measurements
            .OrderBy(x => x.MeasurementDate)
            .ExecuteAsync();

        // Assert
        var ordered = results.OrderBy(x => x.MeasurementDate).ToList();
        Assert.Equal(ordered, results);
    }

    [Fact]
    public async Task OrderBy_InvalidProperty_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => DbContext.Tests
            .OrderBy((TestLalala x) => x.GetHashCode())
            .ExecuteAsync());
    }
}