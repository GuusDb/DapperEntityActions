namespace DapperOrmCore.Tests;

public class SelectTests : TestSetup
{
    [Fact]
    public async Task Select_SingleProperty_ShouldReturnOnlyThatProperty()
    {
        // Act
        var results = await DbContext.Plants
            .Select(p => p.Description)
            .ExecuteAsync();

        // Assert
        Assert.Equal(4, results.Count());
        
        // Check that all plant descriptions are returned
        Assert.Contains("Plant 1", results);
        Assert.Contains("Plant 2", results);
        Assert.Contains("Plant 3", results);
        Assert.Contains("Plant 4", results);
    }

    [Fact]
    public async Task Select_MultipleProperties_ShouldReturnAnonymousType()
    {
        // Act
        var results = await DbContext.Plants
            .Select(p => new { p.PlantCd, p.Description })
            .ExecuteAsync();

        // Assert
        Assert.Equal(4, results.Count());
        
        // Find the first plant and verify its properties
        var plant1 = results.First(p => p.PlantCd == "PLANT1");
        Assert.Equal("Plant 1", plant1.Description);
        
        // Verify all plants are returned
        Assert.Contains(results, p => p.PlantCd == "PLANT2" && p.Description == "Plant 2");
        Assert.Contains(results, p => p.PlantCd == "PLANT3" && p.Description == "Plant 3");
        Assert.Contains(results, p => p.PlantCd == "PLANT4" && p.Description == "Plant 4");
    }

    [Fact]
    public async Task Select_ToAnonymousType_ShouldReturnAnonymousType()
    {
        // Act
        var results = await DbContext.Plants
            .Select(p => new { Code = p.PlantCd, Name = p.Description, IsActive = p.IsAcive })
            .ExecuteAsync();

        // Assert
        Assert.Equal(4, results.Count());
        
        // Verify all plants are returned with correct properties
        Assert.Contains(results, d => d.Code == "PLANT1" && d.Name == "Plant 1" && d.IsActive);
        Assert.Contains(results, d => d.Code == "PLANT2" && d.Name == "Plant 2" && d.IsActive);
        Assert.Contains(results, d => d.Code == "PLANT3" && d.Name == "Plant 3" && !d.IsActive);
        Assert.Contains(results, d => d.Code == "PLANT4" && d.Name == "Plant 4" && d.IsActive);
    }

    [Fact]
    public async Task Select_WithWhere_ShouldFilterAndProject()
    {
        // Act
        var results = await DbContext.Plants
            .Select(p => new { p.PlantCd, p.Description })
            .Where(p => p.PlantCd == "PLANT1" || p.PlantCd == "PLANT2")
            .ExecuteAsync();

        // Assert
        Assert.Equal(2, results.Count());
        
        // Check that only the filtered plants are returned
        Assert.Contains(results, p => p.PlantCd == "PLANT1");
        Assert.Contains(results, p => p.PlantCd == "PLANT2");
        Assert.DoesNotContain(results, p => p.PlantCd == "PLANT3");
        Assert.DoesNotContain(results, p => p.PlantCd == "PLANT4");
    }

    [Fact]
    public async Task Select_WithOrderBy_ShouldOrderAndProject()
    {
        // Act
        var results = await DbContext.Plants
            .Select(p => p.PlantCd)
            .OrderBy(p => p.PlantCd)
            .ExecuteAsync();

        // Assert
        var plantCodes = results.ToList();
        Assert.Equal(4, plantCodes.Count);
        
        // Check that the order is correct (alphabetical by plant code)
        Assert.Equal("PLANT1", plantCodes[0]);
        Assert.Equal("PLANT2", plantCodes[1]);
        Assert.Equal("PLANT3", plantCodes[2]);
        Assert.Equal("PLANT4", plantCodes[3]);
    }
}