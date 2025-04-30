namespace DapperOrmCore.Tests;

public class OneToManyTests : TestSetup
{
    [Fact]
    public async Task Include_Children_ShouldPopulateChildrenList()
    {
        // Act
        var results = await DbContext.Parents
            .Include(x => x.Children)
            .ExecuteAsync();

        // Assert
        var parents = results.ToList();
        Assert.Equal(3, parents.Count);

        var parent1 = parents.First(p => p.ParentId == 1);
        Assert.NotNull(parent1.Children);
        Assert.Equal(2, parent1.Children.Count);
        Assert.Contains(parent1.Children, c => c.Name == "Child1" && c.IsActive);
        Assert.Contains(parent1.Children, c => c.Name == "Child2" && !c.IsActive);

        var parent2 = parents.First(p => p.ParentId == 2);
        Assert.NotNull(parent2.Children);
        Assert.Equal(2, parent2.Children.Count);
        Assert.Contains(parent2.Children, c => c.Name == "Child3" && c.IsActive);
        Assert.Contains(parent2.Children, c => c.Name == "Child4" && c.IsActive);

        var parent3 = parents.First(p => p.ParentId == 3);
        Assert.NotNull(parent3.Children);
        Assert.Equal(1, parent3.Children.Count);
        Assert.Contains(parent3.Children, c => c.Name == "Child5" && !c.IsActive);
    }

    [Fact]
    public async Task Include_Children_WithWhere_ShouldFilterParentsAndPopulateChildren()
    {
        // Act
        var results = await DbContext.Parents
            .Include(x => x.Children)
            .Where(x => x.ParentId == 1)
            .ExecuteAsync();

        // Assert
        var parents = results.ToList();
        Assert.Single(parents);
        var parent = parents.First();
        Assert.Equal(1, parent.ParentId);
        Assert.Equal("Parent1", parent.Name);
        Assert.NotNull(parent.Children);
        Assert.Equal(2, parent.Children.Count);
        Assert.Contains(parent.Children, c => c.Name == "Child1" && c.IsActive);
        Assert.Contains(parent.Children, c => c.Name == "Child2" && !c.IsActive);
    }

    [Fact]
    public async Task Include_Children_WithPagination_ShouldReturnPagedParentsWithChildren()
    {
        // Act
        var results = await DbContext.Parents
            .Include(x => x.Children)
            .OrderBy(x => x.ParentId)
            .Paginate(0, 2)
            .ExecuteAsync();

        // Assert
        var parents = results.ToList();
        Assert.Equal(2, parents.Count);
        Assert.Equal(1, parents[0].ParentId);
        Assert.Equal(2, parents[1].ParentId);
        Assert.Equal(2, parents[0].Children.Count);
        Assert.Equal(2, parents[1].Children.Count);
    }

    [Fact]
    public async Task NoInclude_Children_ShouldNotPopulateChildrenList()
    {
        // Act
        var results = await DbContext.Parents
            .ExecuteAsync();

        // Assert
        var parents = results.ToList();
        Assert.Equal(3, parents.Count);

        Assert.All(parents, p =>
        {
            Assert.NotNull(p.Children);
            Assert.Empty(p.Children);
        });
    }

    [Fact]
    public async Task Where_Children_NameEqualsChild1_ShouldReturnMatchingParents()
    {
        // Act
        var results = await DbContext.Parents
            .Where(x => x.Children.Any(c => c.Name == "Child1"))
            .ExecuteAsync();

        // Assert
        var parents = results.ToList();
        Assert.Single(parents);
        Assert.Equal(1, parents[0].ParentId);
        Assert.Equal("Parent1", parents[0].Name);
        Assert.Empty(parents[0].Children); // Children not included
    }

    [Fact]
    public async Task Where_Children_NameNotEqualsChild1_ShouldReturnMatchingParents()
    {
        // Act
        var results = await DbContext.Parents
            .Where(x => x.Children.Any(c => c.Name != "Child1"))
            .ExecuteAsync();

        // Assert
        var parents = results.ToList();
        Assert.Equal(3, parents.Count); // All parents have at least one child not named "Child1"
        Assert.Contains(parents, p => p.ParentId == 1); // Has "Child2"
        Assert.Contains(parents, p => p.ParentId == 2); // Has "Child3", "Child4"
        Assert.Contains(parents, p => p.ParentId == 3); // Has "Child5"
        Assert.All(parents, p => Assert.Empty(p.Children)); // Children not included
    }

    [Fact]
    public async Task Where_Children_IsActive_ShouldReturnMatchingParents()
    {
        // Act
        var results = await DbContext.Parents
            .Where(x => x.Children.Any(c => c.IsActive))
            .ExecuteAsync();

        // Assert
        var parents = results.ToList();
        Assert.Equal(2, parents.Count); // Parent1 (Child1 active), Parent2 (Child3, Child4 active)
        Assert.Contains(parents, p => p.ParentId == 1);
        Assert.Contains(parents, p => p.ParentId == 2);
        Assert.All(parents, p => Assert.Empty(p.Children)); // Children not included
    }

    [Fact]
    public async Task Where_Children_NameEqualsChild1_WithInclude_ShouldReturnParentsWithChildren()
    {
        // Act
        var results = await DbContext.Parents
            .Include(x => x.Children)
            .Where(x => x.Children.Any(c => c.Name == "Child1"))
            .ExecuteAsync();

        // Assert
        var parents = results.ToList();
        Assert.Single(parents);
        var parent = parents[0];
        Assert.Equal(1, parent.ParentId);
        Assert.Equal("Parent1", parent.Name);
        Assert.NotNull(parent.Children);
        Assert.Equal(2, parent.Children.Count);
        Assert.Contains(parent.Children, c => c.Name == "Child1" && c.IsActive);
        Assert.Contains(parent.Children, c => c.Name == "Child2" && !c.IsActive);
    }

    [Fact]
    public async Task Where_Children_CombinedWithParentCondition_ShouldReturnMatchingParents()
    {
        // Act
        var results = await DbContext.Parents
            .Where(x => x.Children.Any(c => c.IsActive) && x.Name == "Parent2")
            .ExecuteAsync();

        // Assert
        var parents = results.ToList();
        Assert.Single(parents);
        Assert.Equal(2, parents[0].ParentId);
        Assert.Equal("Parent2", parents[0].Name);
        Assert.Empty(parents[0].Children); // Children not included
    }

    [Fact]
    public async Task Where_Children_NoMatches_ShouldReturnEmpty()
    {
        // Act
        var results = await DbContext.Parents
            .Where(x => x.Children.Any(c => c.Name == "NonExistent"))
            .ExecuteAsync();

        // Assert
        Assert.Empty(results);
    }
}