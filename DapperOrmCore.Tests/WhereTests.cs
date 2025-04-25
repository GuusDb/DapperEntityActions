namespace DapperOrmCore.Tests;

public class WhereTests : TestSetup
{
    [Fact]
    public async Task Where_SingleCondition_ShouldReturnMatchingTests()
    {
        // Act
        var results = await DbContext.Tests
            .Where(x => x.TestCd == "TEST1")
            .ExecuteAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal("TEST1", results.First().TestCd);
    }

    [Fact]
    public async Task Where_MultipleConditions_ShouldReturnMatchingMeasurements()
    {
        // Act
        var results = await DbContext.Measurements
            .Where(x => x.Value > 100 && x.TestCd == "TEST1")
            .ExecuteAsync();

        // Assert
        Assert.Equal(2, results.Count());
        Assert.All(results, m => Assert.True(m.Value > 100));
        Assert.All(results, m => Assert.Equal("TEST1", m.TestCd));
    }

    [Fact]
    public async Task Where_BooleanProperty_ShouldReturnMatchingPlants()
    {
        // Act
        var results = await DbContext.Plants
            .Where(x => x.IsAcive)
            .ExecuteAsync();

        // Assert
        Assert.Equal(3, results.Count());
        Assert.All(results, p => Assert.True(p.IsAcive));
    }

    [Fact]
    public async Task Where_NegatedCondition_ShouldReturnMatchingTests()
    {
        // Act
        var results = await DbContext.Tests
            .Where(x => !x.IsActive)
            .ExecuteAsync();

        // Assert
        Assert.Single(results);
        Assert.False(results.First().IsActive);
    }

    [Fact]
    public async Task Where_NoMatches_ShouldReturnEmpty()
    {
        // Act
        var results = await DbContext.Tests
            .Where(x => x.TestCd == "INVALID")
            .ExecuteAsync();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task Where_ContainsMethod_ShouldReturnMatchingTests()
    {
        // Act
        var results = await DbContext.Tests
            .Where(x => x.Description.Contains("Test"))
            .ExecuteAsync();

        // Assert
        Assert.Equal(4, results.Count());
        Assert.All(results, t => Assert.Contains("Test", t.Description));
    }

    [Fact]
    public async Task Where_TestNavigationPropertyDescription_ShouldReturnMatchingMeasurementsWithoutTest()
    {
        // Act
        var results = await DbContext.Measurements
            .Where(x => x.Test.Description == "Test 1")
            .ExecuteAsync();

        // Assert
        Assert.Equal(2, results.Count());
        Assert.All(results, m =>
        {
            Assert.Equal("TEST1", m.TestCd);
            Assert.Null(m.Test); // Test should not be loaded
        });
    }

    [Fact]
    public async Task Where_TestNavigationPropertyIsActive_ShouldReturnMatchingMeasurementsWithoutTest()
    {
        // Act
        var results = await DbContext.Measurements
            .Where(x => x.Test.IsActive)
            .ExecuteAsync();

        // Assert
        Assert.Equal(5, results.Count()); // TEST1 (2), TEST2 (2), TEST4 (1)
        Assert.All(results, m =>
        {
            Assert.True(m.TestCd == "TEST1" || m.TestCd == "TEST2" || m.TestCd == "TEST4");
            Assert.Null(m.Test); // Test should not be loaded
        });
    }

    [Fact]
    public async Task Where_PlantNavigationPropertyIsActive_ShouldReturnMatchingMeasurementsWithoutPlant()
    {
        // Act
        var results = await DbContext.Measurements
            .Where(x => x.Plant.IsAcive)
            .ExecuteAsync();

        // Assert
        Assert.Equal(5, results.Count()); // PLANT1 (2), PLANT2 (2), PLANT4 (1)
        Assert.All(results, m =>
        {
            Assert.True(m.PlantCd == "PLANT1" || m.PlantCd == "PLANT2" || m.PlantCd == "PLANT4");
            Assert.Null(m.Plant); // Plant should not be loaded
        });
    }

    [Fact]
    public async Task IncludeTest_WhereTestDescription_ShouldReturnMeasurementsWithTest()
    {
        // Act
        var results = await DbContext.Measurements
            .Include(x => x.Test)
            .Where(x => x.Test.Description == "Test 1")
            .ExecuteAsync();

        // Assert
        Assert.Equal(2, results.Count());
        Assert.All(results, m =>
        {
            Assert.Equal("TEST1", m.TestCd);
            Assert.NotNull(m.Test);
            Assert.Equal("Test 1", m.Test.Description);
            Assert.Equal(m.TestCd, m.Test.TestCd);
            Assert.Null(m.Plant); // Plant should not be loaded
        });
    }

    [Fact]
    public async Task IncludePlant_WherePlantIsActive_ShouldReturnMeasurementsWithPlant()
    {
        // Act
        var results = await DbContext.Measurements
            .Include(x => x.Plant)
            .Where(x => x.Plant.IsAcive)
            .ExecuteAsync();

        // Assert
        Assert.Equal(5, results.Count()); // PLANT1 (2), PLANT2 (2), PLANT4 (1)
        Assert.All(results, m =>
        {
            Assert.True(m.PlantCd == "PLANT1" || m.PlantCd == "PLANT2" || m.PlantCd == "PLANT4");
            Assert.NotNull(m.Plant);
            Assert.True(m.Plant.IsAcive);
            Assert.Equal(m.PlantCd, m.Plant.PlantCd);
            Assert.Null(m.Test); // Test should not be loaded
        });
    }

    [Fact]
    public async Task IncludeBoth_WhereTestAndPlantProperties_ShouldReturnMeasurementsWithBoth()
    {
        // Act
        var results = await DbContext.Measurements
            .Include(x => x.Test)
            .Include(x => x.Plant)
            .Where(x => x.Test.Description == "Test 1" && x.Plant.IsAcive)
            .ExecuteAsync();

        // Assert
        Assert.Equal(2, results.Count()); // TEST1 with PLANT1, PLANT2
        Assert.All(results, m =>
        {
            Assert.Equal("TEST1", m.TestCd);
            Assert.True(m.PlantCd == "PLANT1" || m.PlantCd == "PLANT2");
            Assert.NotNull(m.Test);
            Assert.Equal("Test 1", m.Test.Description);
            Assert.NotNull(m.Plant);
            Assert.True(m.Plant.IsAcive);
        });
    }

    [Fact]
    public async Task Where_NavigationPropertyNoMatches_ShouldReturnEmpty()
    {
        // Act
        var results = await DbContext.Measurements
            .Where(x => x.Test.Description == "NonExistent")
            .ExecuteAsync();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task Where_CombinedMainAndNavigationProperties_ShouldReturnCorrectMeasurements()
    {
        // Act
        var results = await DbContext.Measurements
            .Where(x => x.Test.Description == "Test 1" && x.Value > 150.5)
            .ExecuteAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(200.0, results.First().Value);
        Assert.Equal("TEST1", results.First().TestCd);
        Assert.Null(results.First().Test); // Test should not be loaded
    }
}