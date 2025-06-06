using DapperOrmCore.Tests.Models;
using System.Threading.Tasks;
using Xunit;

namespace DapperOrmCore.Tests;

public class CombinationTests : TestSetup
{
    [Fact]
    public async Task Select_ThenWhere_ShouldProjectThenFilter()
    {
        // Act
        var results = await DbContext.Plants
            .Select(p => new { p.PlantCd, p.Description, p.IsAcive })
            .Where(p => p.IsAcive)
            .ExecuteAsync();

        // Assert
        Assert.Equal(3, results.Count());
        
        // Check that only active plants are returned
        Assert.Contains(results, p => p.PlantCd == "PLANT1" && p.IsAcive);
        Assert.Contains(results, p => p.PlantCd == "PLANT2" && p.IsAcive);
        Assert.Contains(results, p => p.PlantCd == "PLANT4" && p.IsAcive);
        Assert.DoesNotContain(results, p => p.PlantCd == "PLANT3");
    }

    [Fact]
    public async Task Where_ThenSelect_ShouldFilterThenProject()
    {
        // Act
        var results = await DbContext.Plants
            .Where(p => p.IsAcive)
            .Select(p => new { Code = p.PlantCd, Name = p.Description })
            .ExecuteAsync();

        // Assert
        Assert.Equal(3, results.Count());
        
        // Check that only active plants are returned
        Assert.Contains(results, d => d.Code == "PLANT1" && d.Name == "Plant 1");
        Assert.Contains(results, d => d.Code == "PLANT2" && d.Name == "Plant 2");
        Assert.Contains(results, d => d.Code == "PLANT4" && d.Name == "Plant 4");
        Assert.DoesNotContain(results, d => d.Code == "PLANT3");
    }

    [Fact]
    public async Task Select_ThenOrderBy_ShouldProjectThenOrder()
    {
        // Act
        var results = await DbContext.Plants
            .Select(p => new { p.PlantCd, p.Description })
            .OrderBy(p => p.Description)
            .ExecuteAsync();

        // Assert
        var items = results.ToList();
        Assert.Equal(4, items.Count);
        
        // Check that the order is correct (alphabetical by name/description)
        Assert.Equal("PLANT1", items[0].PlantCd);
        Assert.Equal("PLANT2", items[1].PlantCd);
        Assert.Equal("PLANT3", items[2].PlantCd);
        Assert.Equal("PLANT4", items[3].PlantCd);
    }

    [Fact]
    public async Task OrderBy_ThenSelect_ShouldOrderThenProject()
    {
        // Act
        var results = await DbContext.Plants
            .OrderBy(p => p.Description)
            .Select(p => p.PlantCd)
            .ExecuteAsync();

        // Assert
        var plantCodes = results.ToList();
        Assert.Equal(4, plantCodes.Count);
        
        // Check that the order is correct (alphabetical by description)
        Assert.Equal("PLANT1", plantCodes[0]);
        Assert.Equal("PLANT2", plantCodes[1]);
        Assert.Equal("PLANT3", plantCodes[2]);
        Assert.Equal("PLANT4", plantCodes[3]);
    }

    [Fact]
    public async Task Select_Where_OrderBy_ShouldCombineAllOperations()
    {
        // Act
        var results = await DbContext.Plants
            .Select(p => new { p.PlantCd, p.Description, p.IsAcive })
            .Where(p => p.IsAcive)
            .OrderBy(p => p.Description, descending: true)
            .ExecuteAsync();

        // Assert
        Assert.Equal(3, results.Count());
        
        var items = results.ToList();
        
        // Check that only active plants are returned in descending order
        Assert.Equal("PLANT4", items[0].PlantCd); // Plant 4 should be first (descending order)
        Assert.Equal("PLANT2", items[1].PlantCd);
        Assert.Equal("PLANT1", items[2].PlantCd);
        
        // Check that inactive plants are not included
        Assert.DoesNotContain(results, i => i.PlantCd == "PLANT3");
    }

    [Fact]
    public async Task Select_ThenPaginate_ShouldProjectThenPaginate()
    {
        // Act
        var results = await DbContext.Plants
            .Select(p => new { p.PlantCd, p.Description })
            .Paginate(0, 2) // First page, 2 items per page
            .ExecuteAsync();

        // Assert
        Assert.Equal(2, results.Count());
        
        var items = results.ToList();
        Assert.Equal("PLANT1", items[0].PlantCd);
        Assert.Equal("PLANT2", items[1].PlantCd);
    }

    [Fact]
    public async Task Where_OrderBy_ShouldFilterThenOrder()
    {
        // Act
        var results = await DbContext.Plants
            .Where(p => p.IsAcive)
            .OrderBy(p => p.Description)
            .ExecuteAsync();

        // Assert
        var plants = results.ToList();
        Assert.Equal(3, plants.Count);
        
        // Check that only active plants are returned in the correct order
        Assert.Equal("PLANT1", plants[0].PlantCd);
        Assert.Equal("PLANT2", plants[1].PlantCd);
        Assert.Equal("PLANT4", plants[2].PlantCd);
        
        // Check that inactive plants are not included
        Assert.DoesNotContain(plants, p => p.PlantCd == "PLANT3");
    }

    [Fact]
    public async Task Where_Paginate_ShouldFilterThenPaginate()
    {
        // Act
        var results = await DbContext.Plants
            .Where(p => p.IsAcive)
            .Paginate(0, 2) // First page, 2 items per page
            .ExecuteAsync();

        // Assert
        var plants = results.ToList();
        Assert.Equal(2, plants.Count);
        
        // Check that only the first 2 active plants are returned
        Assert.Equal("PLANT1", plants[0].PlantCd);
        Assert.Equal("PLANT2", plants[1].PlantCd);
        
        // Check second page
        var page2Results = await DbContext.Plants
            .Where(p => p.IsAcive)
            .Paginate(1, 2) // Second page, 2 items per page
            .ExecuteAsync();
            
        var page2Plants = page2Results.ToList();
        Assert.Single(page2Plants);
        Assert.Equal("PLANT4", page2Plants[0].PlantCd);
    }

    [Fact]
    public async Task OrderBy_Paginate_ShouldOrderThenPaginate()
    {
        // Act
        var results = await DbContext.Plants
            .OrderBy(p => p.PlantCd, descending: true)
            .Paginate(0, 2) // First page, 2 items per page
            .ExecuteAsync();

        // Assert
        var plants = results.ToList();
        Assert.Equal(2, plants.Count);
        
        // Check that plants are returned in descending order by PlantCd
        Assert.Equal("PLANT4", plants[0].PlantCd);
        Assert.Equal("PLANT3", plants[1].PlantCd);
        
        // Check second page
        var page2Results = await DbContext.Plants
            .OrderBy(p => p.PlantCd, descending: true)
            .Paginate(1, 2) // Second page, 2 items per page
            .ExecuteAsync();
            
        var page2Plants = page2Results.ToList();
        Assert.Equal(2, page2Plants.Count);
        Assert.Equal("PLANT2", page2Plants[0].PlantCd);
        Assert.Equal("PLANT1", page2Plants[1].PlantCd);
    }

    [Fact]
    public async Task Where_OrderBy_Paginate_ShouldCombineAllOperations()
    {
        // Act
        var results = await DbContext.Plants
            .Where(p => p.IsAcive)
            .OrderBy(p => p.Description, descending: true)
            .Paginate(0, 2) // First page, 2 items per page
            .ExecuteAsync();

        // Assert
        var plants = results.ToList();
        Assert.Equal(2, plants.Count);
        
        // Check that only active plants are returned in descending order by Description
        Assert.Equal("PLANT4", plants[0].PlantCd); // Plant 4 should be first (descending order)
        Assert.Equal("PLANT2", plants[1].PlantCd);
        
        // Check second page
        var page2Results = await DbContext.Plants
            .Where(p => p.IsAcive)
            .OrderBy(p => p.Description, descending: true)
            .Paginate(1, 2) // Second page, 2 items per page
            .ExecuteAsync();
            
        var page2Plants = page2Results.ToList();
        Assert.Single(page2Plants);
        Assert.Equal("PLANT1", page2Plants[0].PlantCd);
    }

    [Fact]
    public async Task Include_Where_ShouldIncludeRelatedEntitiesAndFilter()
    {
        // Act
        var results = await DbContext.Measurements
            .Include(m => m.Test)
            .Where(m => m.TestCd == "TEST1")
            .ExecuteAsync();

        // Assert
        var measurements = results.ToList();
        Assert.Equal(2, measurements.Count);
        
        // Check that only measurements with TestCd = TEST1 are returned
        Assert.All(measurements, m => Assert.Equal("TEST1", m.TestCd));
        
        // Check that related Test entities are included
        Assert.All(measurements, m =>
        {
            Assert.NotNull(m.Test);
            Assert.Equal("Test 1", m.Test.Description);
        });
    }
}