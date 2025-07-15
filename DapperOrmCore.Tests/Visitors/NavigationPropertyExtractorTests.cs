using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DapperOrmCore.Tests.Models;
using DapperOrmCore.Visitors;
using Xunit;

namespace DapperOrmCore.Tests.Visitors
{
    public class NavigationPropertyExtractorTests
    {
        [Fact]
        public void Extract_WithNoNavigationProperties_ShouldReturnEmptyCollection()
        {
            // Arrange
            var navigationProperties = new List<string> { "Test", "Plant" };
            var extractor = new NavigationPropertyExtractor(navigationProperties);
            Expression<Func<CoolMeasurement, bool>> predicate = m => m.Value > 100;

            // Act
            var result = extractor.Extract(predicate);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void Extract_WithSingleNavigationProperty_ShouldExtractCorrectly()
        {
            // Arrange
            var navigationProperties = new List<string> { "Test", "Plant" };
            var extractor = new NavigationPropertyExtractor(navigationProperties);
            Expression<Func<CoolMeasurement, bool>> predicate = m => m.Test.Description == "Test 1";

            // Act
            var result = extractor.Extract(predicate).ToList();

            // Assert
            Assert.Single(result);
            Assert.Contains("Test", result);
        }

        [Fact]
        public void Extract_WithMultipleNavigationProperties_ShouldExtractAll()
        {
            // Arrange
            var navigationProperties = new List<string> { "Test", "Plant" };
            var extractor = new NavigationPropertyExtractor(navigationProperties);
            Expression<Func<CoolMeasurement, bool>> predicate =
                m => m.Test.Description == "Test 1" && m.Plant.IsAcive;

            // Act
            var result = extractor.Extract(predicate).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains("Test", result);
            Assert.Contains("Plant", result);
        }

        [Fact]
        public void Extract_WithDuplicateNavigationProperties_ShouldReturnDistinctSet()
        {
            // Arrange
            var navigationProperties = new List<string> { "Test", "Plant" };
            var extractor = new NavigationPropertyExtractor(navigationProperties);
            Expression<Func<CoolMeasurement, bool>> predicate =
                m => m.Test.Description == "Test 1" && m.Test.IsActive;

            // Act
            var result = extractor.Extract(predicate).ToList();

            // Assert
            Assert.Single(result);
            Assert.Contains("Test", result);
        }

        [Fact]
        public void Extract_WithNestedConditions_ShouldExtractAllNavigationProperties()
        {
            // Arrange
            var navigationProperties = new List<string> { "Test", "Plant" };
            var extractor = new NavigationPropertyExtractor(navigationProperties);
            Expression<Func<CoolMeasurement, bool>> predicate =
                m => (m.Test.Description == "Test 1" || m.Test.IsActive) && m.Plant.IsAcive;

            // Act
            var result = extractor.Extract(predicate).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains("Test", result);
            Assert.Contains("Plant", result);
        }

        [Fact]
        public void Extract_WithNonMatchingNavigationProperties_ShouldIgnoreThem()
        {
            // Arrange
            var navigationProperties = new List<string> { "NonExistent" };
            var extractor = new NavigationPropertyExtractor(navigationProperties);
            Expression<Func<CoolMeasurement, bool>> predicate = m => m.Test.Description == "Test 1";

            // Act
            var result = extractor.Extract(predicate);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void Extract_WithParentChildRelationship_ShouldExtractCorrectly()
        {
            // Arrange
            var navigationProperties = new List<string> { "Children" };
            var extractor = new NavigationPropertyExtractor(navigationProperties);

            Expression<Func<Parent, bool>> predicate = p => p.Children.Count > 0 || p.Children[0].Name == "Child1";

            // Act
            var result = extractor.Extract(predicate).ToList();

            // Assert
            Assert.Single(result);
            Assert.Contains("Children", result);
        }

        [Fact]
        public void Extract_WithMultipleLevelsOfNavigationProperties_ShouldExtractCorrectly()
        {
            // Arrange
            // This test simulates a scenario where we have multiple levels of navigation properties
            // For example: Order -> Customer -> Address
            // We'll use the existing models for simplicity
            var navigationProperties = new List<string> { "Test", "Plant" };
            var extractor = new NavigationPropertyExtractor(navigationProperties);

            // Create a more complex expression that would represent accessing a property through
            // multiple navigation properties if our model supported it
            Expression<Func<CoolMeasurement, bool>> predicate = m =>
                m.Test.Description == "Test 1" &&
                m.Plant.IsAcive &&
                (m.Value > 100 || m.TestCd == "TEST1");

            // Act
            var result = extractor.Extract(predicate).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains("Test", result);
            Assert.Contains("Plant", result);
        }
    }
}