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
    public class WhereExpressionVisitorStringMethodsTests
    {
        private readonly Dictionary<string, PropertyInfo> _propertyMap;
        private readonly Dictionary<string, NavigationPropertyInfo> _navigationProperties;
        private readonly List<string> _referencedNavProps;

        public WhereExpressionVisitorStringMethodsTests()
        {
            _propertyMap = typeof(Lod).GetProperties()
                .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null)
                .ToDictionary(
                    p => p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name,
                    p => p,
                    StringComparer.OrdinalIgnoreCase);

            _navigationProperties = new Dictionary<string, NavigationPropertyInfo>();
            _referencedNavProps = new List<string>();
        }

        [Fact]
        public void Translate_StartsWith_ShouldGenerateCorrectSql()
        {
            // Arrange
            var visitor = new WhereExpressionVisitor<Lod>(_propertyMap, _navigationProperties, _referencedNavProps);
            Expression<Func<Lod, bool>> expression = l => l.LodCd.StartsWith("ABC");

            // Act
            var (sql, parameters) = visitor.Translate(expression);

            // Assert
            Assert.Contains("t.lod_cd", sql);
            Assert.Contains("LIKE @", sql);
            Assert.Equal("ABC%", parameters.Get<object>("p0"));
        }

        [Fact]
        public void Translate_EndsWith_ShouldGenerateCorrectSql()
        {
            // Arrange
            var visitor = new WhereExpressionVisitor<Lod>(_propertyMap, _navigationProperties, _referencedNavProps);
            Expression<Func<Lod, bool>> expression = l => l.LodCd.EndsWith("XYZ");

            // Act
            var (sql, parameters) = visitor.Translate(expression);

            // Assert
            Assert.Contains("t.lod_cd", sql);
            Assert.Contains("LIKE @", sql);
            Assert.Equal("%XYZ", parameters.Get<object>("p0"));
        }

        [Fact]
        public void Translate_ToLower_ShouldGenerateCorrectSql()
        {
            // Arrange
            var visitor = new WhereExpressionVisitor<Lod>(_propertyMap, _navigationProperties, _referencedNavProps);
            Expression<Func<Lod, bool>> expression = l => l.LodCd.ToLower() == "abc";

            // Act
            var (sql, parameters) = visitor.Translate(expression);

            // Assert
            Assert.Contains("LOWER(t.lod_cd)", sql);
            Assert.Contains("= @", sql);
            Assert.Equal("abc", parameters.Get<object>("p0"));
        }

        [Fact]
        public void Translate_ToUpper_ShouldGenerateCorrectSql()
        {
            // Arrange
            var visitor = new WhereExpressionVisitor<Lod>(_propertyMap, _navigationProperties, _referencedNavProps);
            Expression<Func<Lod, bool>> expression = l => l.LodCd.ToUpper() == "ABC";

            // Act
            var (sql, parameters) = visitor.Translate(expression);

            // Assert
            Assert.Contains("UPPER(t.lod_cd)", sql);
            Assert.Contains("= @", sql);
            Assert.Equal("ABC", parameters.Get<object>("p0"));
        }

        [Fact]
        public void Translate_ToLowerStartsWith_ShouldGenerateCorrectSql()
        {
            // Arrange
            var visitor = new WhereExpressionVisitor<Lod>(_propertyMap, _navigationProperties, _referencedNavProps);
            Expression<Func<Lod, bool>> expression = l => l.LodCd.ToLower().StartsWith("abc");

            // Act
            var (sql, parameters) = visitor.Translate(expression);

            // Assert
            Assert.Contains("LOWER(t.lod_cd)", sql);
            Assert.Contains("LIKE @", sql);
            Assert.Equal("abc%", parameters.Get<object>("p0"));
        }

        [Fact]
        public void Translate_ToLowerInvariantStartsWith_ShouldGenerateCorrectSql()
        {
            // Arrange
            var visitor = new WhereExpressionVisitor<Lod>(_propertyMap, _navigationProperties, _referencedNavProps);
            string search = "abc";
            Expression<Func<Lod, bool>> expression = l => l.LodCd.ToLowerInvariant().StartsWith(search.ToLowerInvariant());

            // Act
            var (sql, parameters) = visitor.Translate(expression);

            // Assert
            Assert.Contains("LOWER(t.lod_cd)", sql);
            Assert.Contains("LIKE @", sql);
            Assert.Equal("abc%", parameters.Get<object>("p0"));
        }

        [Fact]
        public void Translate_ComplexStringOperations_ShouldGenerateCorrectSql()
        {
            // Arrange
            var visitor = new WhereExpressionVisitor<Lod>(_propertyMap, _navigationProperties, _referencedNavProps);
            
            // Use two separate tests to ensure each part works correctly
            Expression<Func<Lod, bool>> expression1 = l =>
                l.IsActive && l.LodCd.ToLower().StartsWith("abc");
                
            Expression<Func<Lod, bool>> expression2 = l =>
                l.IsActive && l.Description != null && l.Description.ToUpper().Contains("XYZ");

            // Act
            var (sql1, parameters1) = visitor.Translate(expression1);
            var (sql2, parameters2) = visitor.Translate(expression2);

            // Assert for first expression
            Assert.Contains("t.is_active = @", sql1);
            // With null checks, the SQL might include IS NOT NULL, so we check for partial matches
            Assert.Contains("LOWER(t.lod_cd)", sql1);
            Assert.Contains("LIKE @", sql1);
            
            bool foundStartsWithParam = false;
            foreach (var paramName in parameters1.ParameterNames)
            {
                var value = parameters1.Get<object>(paramName);
                if (value is string strValue && strValue == "abc%")
                {
                    foundStartsWithParam = true;
                    break;
                }
            }
            Assert.True(foundStartsWithParam, "Should have a parameter with value 'abc%'");
            
            // Assert for second expression
            Assert.Contains("t.is_active = @", sql2);
            // With null checks, the SQL might include IS NOT NULL, so we check for partial matches
            Assert.Contains("UPPER(t.description)", sql2);
            Assert.Contains("LIKE @", sql2);
            
            bool foundContainsParam = false;
            foreach (var paramName in parameters2.ParameterNames)
            {
                var value = parameters2.Get<object>(paramName);
                if (value is string strValue && strValue == "%XYZ%")
                {
                    foundContainsParam = true;
                    break;
                }
            }
            Assert.True(foundContainsParam, "Should have a parameter with value '%XYZ%'");
        }

        [Fact]
        public void Translate_ExactQueryExample_ShouldGenerateCorrectSql()
        {
            // Arrange
            var visitor = new WhereExpressionVisitor<Lod>(_propertyMap, _navigationProperties, _referencedNavProps);
            string search = "test";
            Expression<Func<Lod, bool>> expression = x => x.LodCd.ToLower().StartsWith(search.ToLower());

            // Act
            var (sql, parameters) = visitor.Translate(expression);

            // Assert
            Assert.Contains("LOWER(t.lod_cd)", sql);
            Assert.Contains("LIKE @", sql);
            Assert.Equal("test%", parameters.Get<object>("p0"));
        }

        [Fact]
        public void Translate_NullableStringProperty_ShouldAddNullCheck()
        {
            // Arrange
            var visitor = new WhereExpressionVisitor<Lod>(_propertyMap, _navigationProperties, _referencedNavProps);
            Expression<Func<Lod, bool>> expression = x => x.Description != null && x.Description.ToLower().StartsWith("test");

            // Act
            var (sql, parameters) = visitor.Translate(expression);

            // Assert
            // The expression is translated as two separate conditions:
            // 1. Description != null
            // 2. Description.ToLower().StartsWith("test") which includes its own null check
            Assert.Contains("t.description != @", sql);
            Assert.Contains("t.description", sql);
            Assert.Contains("LOWER", sql);
            Assert.Contains("LIKE @", sql);
            
            bool foundStartsWithParam = false;
            foreach (var paramName in parameters.ParameterNames)
            {
                var value = parameters.Get<object>(paramName);
                if (value is string strValue && strValue == "test%")
                {
                    foundStartsWithParam = true;
                    break;
                }
            }
            Assert.True(foundStartsWithParam, "Should have a parameter with value 'test%'");
        }

        [Fact]
        public void Translate_NullableStringPropertyWithToLower_ShouldAddNullCheck()
        {
            // Arrange
            var visitor = new WhereExpressionVisitor<Lod>(_propertyMap, _navigationProperties, _referencedNavProps);
            Expression<Func<Lod, bool>> expression = x => x.Description.ToLower() == "test";

            // Act
            var (sql, parameters) = visitor.Translate(expression);

            // Assert
            Assert.Contains("t.description IS NOT NULL", sql);
            Assert.Contains("LOWER(t.description)", sql);
            Assert.Contains("= @", sql);
            Assert.Equal("test", parameters.Get<object>("p0"));
        }
    }
}