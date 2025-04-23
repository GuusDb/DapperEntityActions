using DapperOrmCore.Tests.Models;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DapperOrmCore.Tests;

public class CombinationTests : TestSetup
{
    [Fact]
    public async Task Where_And_OrderBy_ShouldReturnOrderedFilteredTests()
    {
        // Act
        var results = await DbContext.Tests
            .Where(x => x.IsActive)
            .OrderBy(x => x.TestCd)
            .ExecuteAsync();

        // Assert
        Assert.Equal(3, results.Count());
        Assert.All(results, t => Assert.True(t.IsActive));
        Assert.Equal(results.OrderBy(x => x.TestCd), results);
    }

    [Fact]
    public async Task Where_And_Pagination_ShouldReturnPagedFilteredMeasurements()
    {
        // Act
        var results = await DbContext.Measurements
            .Where(x => x.Value > 100)
            .Paginate(0, 2)
            .ExecuteAsync();

        // Assert
        Assert.Equal(2, results.Count());
        Assert.All(results, m => Assert.True(m.Value > 100));
    }

    [Fact]
    public async Task OrderBy_And_Pagination_ShouldReturnPagedOrderedTests()
    {
        // Act
        var results = await DbContext.Tests
            .OrderBy(x => x.TestCd, descending: true)
            .Paginate(0, 2)
            .ExecuteAsync();

        // Assert
        Assert.Equal(2, results.Count());
        Assert.Equal("TEST4", results.First().TestCd);
    }

    [Fact]
    public async Task Where_OrderBy_Pagination_ShouldReturnCorrectMeasurements()
    {
        // Act
        var results = await DbContext.Measurements
            .Where(x => x.Value > 100)
            .OrderBy(x => x.Value)
            .Paginate(0, 2)
            .ExecuteAsync();

        // Assert
        Assert.Equal(2, results.Count());
        Assert.Equal(150.5, results.First().Value);
        Assert.All(results, m => Assert.True(m.Value > 100));
    }

    [Fact]
    public async Task Include_SingleNavigation_ShouldReturnMeasurementsWithTest()
    {
        // Act
        var results = await DbContext.Measurements
            .Include(x => x.Test)
            .Where(x => x.Value > 100)
            .ExecuteAsync();

        // Assert
        Assert.All(results, m =>
        {
            Assert.NotNull(m.Test);
            Assert.Equal(m.TestCd, m.Test.TestCd);
            Assert.True(m.Value > 100);
        });
    }

    [Fact]
    public async Task Include_MultipleNavigation_ShouldReturnMeasurementsWithTestAndPlant()
    {
        // Act
        var results = await DbContext.Measurements
            .Include(x => x.Test)
            .Include(x => x.Plant)
            .Where(x => x.Value > 100)
            .OrderBy(x => x.MeasurementDate)
            .Paginate(0, 2)
            .ExecuteAsync();

        // Assert
        Assert.Equal(2, results.Count());
        Assert.All(results, m =>
        {
            Assert.NotNull(m.Test);
            Assert.NotNull(m.Plant);
            Assert.Equal(m.TestCd, m.Test.TestCd);
            Assert.Equal(m.PlantCd, m.Plant.PlantCd);
            Assert.True(m.Value > 100);
        });
    }

    [Fact]
    public async Task ComplexQuery_WithAllFeatures_ShouldReturnCorrectResults()
    {
        // Act
        var results = await DbContext.Measurements
            .Include(x => x.Test)
            .Include(x => x.Plant)
            .Where(x => x.Value > 100 && x.TestCd == "TEST1")
            .OrderBy(x => x.Value)
            .OrderBy(x => x.MeasurementDate, descending: true)
            .Paginate(0, 1)
            .ExecuteAsync();

        // Assert
        Assert.Single(results);
        var measurement = results.First();
        Assert.Equal(150.5, measurement.Value);
        Assert.Equal("TEST1", measurement.TestCd);
        Assert.Equal("PLANT1", measurement.PlantCd);
        Assert.NotNull(measurement.Test);
        Assert.NotNull(measurement.Plant);
    }

    [Fact]
    public async Task Include_NonExistentNavigationProperty_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => DbContext.Measurements
            .Include((CoolMeasurement x) => x.Id)
            .ExecuteAsync());
    }
}