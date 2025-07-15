using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using Dapper;
using DapperOrmCore.Models;
using DapperOrmCore.Tests.Models;
using DapperOrmCore.Visitors;
using Xunit;

namespace DapperOrmCore.Tests.Visitors
{
    public class WhereExpressionVisitorCollectionTests
    {
        private readonly Dictionary<string, PropertyInfo> _propertyMap;
        private readonly Dictionary<string, NavigationPropertyInfo> _navigationProperties;
        private readonly List<string> _referencedNavProps;

        public WhereExpressionVisitorCollectionTests()
        {
            // Setup property map for Parent
            _propertyMap = typeof(Parent).GetProperties()
                .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null)
                .ToDictionary(
                    p => p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name,
                    p => p,
                    StringComparer.OrdinalIgnoreCase);

            // Setup navigation properties for Parent.Children collection
            _navigationProperties = new Dictionary<string, NavigationPropertyInfo>
            {
                {
                    "Children", new NavigationPropertyInfo
                    {
                        RelatedTableName = "child",
                        RelatedType = typeof(Child),
                        ForeignKeyColumn = "parent_id",
                        Property = typeof(Parent).GetProperty("Children") ?? throw new InvalidOperationException("Children property not found on Parent"),
                        IsCollection = true
                    }
                }
            };

            _referencedNavProps = new List<string> { "Children" };
        }

        [Fact]
        public void Translate_CollectionAny_ShouldGenerateExistsClause()
        {
            // Arrange
            var visitor = new WhereExpressionVisitor<Parent>(_propertyMap, _navigationProperties, _referencedNavProps);
            Expression<Func<Parent, bool>> expression = p => p.Children.Any();

            // Act
            var (sql, parameters) = visitor.Translate(expression);

            // Assert
            Assert.Contains("EXISTS (SELECT 1 FROM child c WHERE c.parent_id = t.parent_id)", sql);
        }

        [Fact]
        public void Translate_CollectionAnyWithPredicate_ShouldGenerateExistsClauseWithCondition()
        {
            // Arrange
            var visitor = new WhereExpressionVisitor<Parent>(_propertyMap, _navigationProperties, _referencedNavProps);
            Expression<Func<Parent, bool>> expression = p => p.Children.Any(c => c.Name == "Child1");

            // Act
            var (sql, parameters) = visitor.Translate(expression);

            // Assert
            Assert.Contains("EXISTS (SELECT 1 FROM child c WHERE c.parent_id = t.parent_id AND", sql);
            Assert.Contains("c.name = @", sql);
            Assert.Equal("Child1", parameters.Get<object>(parameters.ParameterNames.First()));
        }

        [Fact]
        public void Translate_CollectionAnyWithBooleanPredicate_ShouldGenerateExistsClauseWithCondition()
        {
            // Arrange
            var visitor = new WhereExpressionVisitor<Parent>(_propertyMap, _navigationProperties, _referencedNavProps);
            Expression<Func<Parent, bool>> expression = p => p.Children.Any(c => c.IsActive);

            // Act
            var (sql, parameters) = visitor.Translate(expression);

            // Assert
            Assert.Contains("EXISTS (SELECT 1 FROM child c WHERE c.parent_id = t.parent_id AND", sql);
            Assert.Contains("c.is_active = @", sql);
            Assert.Equal(true, parameters.Get<object>(parameters.ParameterNames.First()));
        }

        [Fact]
        public void Translate_CollectionAnyWithComplexPredicate_ShouldGenerateExistsClauseWithCondition()
        {
            // Arrange
            var visitor = new WhereExpressionVisitor<Parent>(_propertyMap, _navigationProperties, _referencedNavProps);
            // Simplify the expression to avoid the null check which adds an extra parameter
            Expression<Func<Parent, bool>> expression = p => p.Children.Any(c => c.Name.Contains("Child") && c.IsActive);

            // Act
            var (sql, parameters) = visitor.Translate(expression);

            // Assert
            Assert.Contains("EXISTS (SELECT 1 FROM child c WHERE c.parent_id = t.parent_id AND", sql);
            Assert.Contains("c.name LIKE @", sql);
            Assert.Contains("AND c.is_active = @", sql);
            
            // Check parameters - order might vary
            var paramNames = parameters.ParameterNames.ToList();
            Assert.Equal(2, paramNames.Count);
            
            // One parameter should be %Child% and one should be true
            bool foundLikeParam = false;
            bool foundBoolParam = false;
            
            foreach (var paramName in paramNames)
            {
                var value = parameters.Get<object>(paramName);
                if (value is string strValue && strValue == "%Child%")
                    foundLikeParam = true;
                else if (value is bool boolValue && boolValue)
                    foundBoolParam = true;
            }
            
            Assert.True(foundLikeParam, "Should have a LIKE parameter with value '%Child%'");
            Assert.True(foundBoolParam, "Should have a boolean parameter with value 'true'");
        }

        [Fact]
        public void Translate_NotCollectionAny_ShouldGenerateNotExistsClause()
        {
            // Arrange
            var visitor = new WhereExpressionVisitor<Parent>(_propertyMap, _navigationProperties, _referencedNavProps);
            Expression<Func<Parent, bool>> expression = p => !p.Children.Any();

            // Act
            var (sql, parameters) = visitor.Translate(expression);

            // Assert
            Assert.Contains("NOT EXISTS (SELECT 1 FROM child c WHERE c.parent_id = t.parent_id)", sql);
        }

        [Fact]
        public void Translate_NotCollectionAnyWithPredicate_ShouldGenerateNotExistsClauseWithCondition()
        {
            // Arrange
            var visitor = new WhereExpressionVisitor<Parent>(_propertyMap, _navigationProperties, _referencedNavProps);
            Expression<Func<Parent, bool>> expression = p => !p.Children.Any(c => c.Name == "Child1");

            // Act
            var (sql, parameters) = visitor.Translate(expression);

            // Assert
            Assert.Contains("NOT EXISTS (SELECT 1 FROM child c WHERE c.parent_id = t.parent_id AND", sql);
            Assert.Contains("c.name = @", sql);
            Assert.Equal("Child1", parameters.Get<object>(parameters.ParameterNames.First()));
        }

        [Fact]
        public void Translate_CombinedWithRegularCondition_ShouldGenerateCorrectSql()
        {
            // Arrange
            var visitor = new WhereExpressionVisitor<Parent>(_propertyMap, _navigationProperties, _referencedNavProps);
            Expression<Func<Parent, bool>> expression = p => p.Name == "Parent1" && p.Children.Any(c => c.IsActive);

            // Act
            var (sql, parameters) = visitor.Translate(expression);

            // Assert
            Assert.Contains("(t.name = @", sql);
            Assert.Contains("AND EXISTS (SELECT 1 FROM child c WHERE c.parent_id = t.parent_id AND", sql);
            Assert.Contains("c.is_active = @", sql);
            
            // Check parameters - order might vary
            var paramNames = parameters.ParameterNames.ToList();
            Assert.Equal(2, paramNames.Count);
            
            // One parameter should be "Parent1" and one should be true
            bool foundNameParam = false;
            bool foundBoolParam = false;
            
            foreach (var paramName in paramNames)
            {
                var value = parameters.Get<object>(paramName);
                if (value is string strValue && strValue == "Parent1")
                    foundNameParam = true;
                else if (value is bool boolValue && boolValue)
                    foundBoolParam = true;
            }
            
            Assert.True(foundNameParam, "Should have a parameter with value 'Parent1'");
            Assert.True(foundBoolParam, "Should have a boolean parameter with value 'true'");
        }

        [Fact]
        public void Translate_NestedCollectionPredicates_ShouldGenerateCorrectSql()
        {
            // Arrange
            var visitor = new WhereExpressionVisitor<Parent>(_propertyMap, _navigationProperties, _referencedNavProps);
            Expression<Func<Parent, bool>> expression = p => p.Children.Any(c => c.Name.Contains("Child") || c.ChildId > 10);

            // Act
            var (sql, parameters) = visitor.Translate(expression);

            // Assert
            Assert.Contains("EXISTS (SELECT 1 FROM child c WHERE c.parent_id = t.parent_id AND", sql);
            
            // Check for the OR condition in the subquery
            Assert.Contains("OR", sql);
            
            // Check for the LIKE condition
            Assert.Contains("c.name LIKE @", sql);
            
            // Check for the > condition
            Assert.Contains("c.child_id > @", sql);
            
            // Check parameters - order might vary
            var paramNames = parameters.ParameterNames.ToList();
            Assert.Equal(2, paramNames.Count);
            
            // One parameter should be %Child% and one should be 10
            bool foundLikeParam = false;
            bool foundNumberParam = false;
            
            foreach (var paramName in paramNames)
            {
                var value = parameters.Get<object>(paramName);
                if (value is string strValue && strValue == "%Child%")
                    foundLikeParam = true;
                else if (value is int intValue && intValue == 10)
                    foundNumberParam = true;
            }
            
            Assert.True(foundLikeParam, "Should have a LIKE parameter with value '%Child%'");
            Assert.True(foundNumberParam, "Should have a numeric parameter with value '10'");
        }
    }
}