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
}