using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;
using DapperOrmCore.Interceptors;
using DapperOrmCore.Tests.Models;
using Xunit;

namespace DapperOrmCore.Tests.Interceptors;

public class InterceptorTests : TestSetup
{
    [Fact]
    public async Task AuditableEntityInterceptor_ShouldSetCreatedOnBeforeInsert()
    {
        // Arrange
        var entity = new AuditableEntity
        {
            Name = "Test Entity",
        };

        var beforeInsert = DateTime.UtcNow;

        // Act
        var insertedId = await DbContext.AuditableEntities.InsertAsync<int>(entity);
        var retrievedEntity = await DbContext.AuditableEntities.GetByIdAsync<int>(insertedId);

        // Assert
        Assert.NotNull(retrievedEntity);
        Assert.Equal("Test Entity", retrievedEntity!.Name);
        
        // CreatedOn should be set by the interceptor
        Assert.NotEqual(default, retrievedEntity!.CreatedDate);
        
        // CreatedOn should be approximately the current time
        var timeDifference = (retrievedEntity!.CreatedDate - beforeInsert).TotalSeconds;
        Assert.True(timeDifference >= 0);
        Assert.True(timeDifference < 5); 
    }

    [Fact]
    public async Task AuditableEntityInterceptor_ShouldNotChangeCreatedOnDuringUpdate()
    {
        // Arrange
        var entity = new AuditableEntity
        {
            Name = "Test Entity",
            // CreatedOn is not set
        };

        // Insert the entity first
        var insertedId = await DbContext.AuditableEntities.InsertAsync<int>(entity);
        var insertedEntity = await DbContext.AuditableEntities.GetByIdAsync<int>(insertedId);
        
        // Store the original CreatedOn value
        Assert.NotNull(insertedEntity);
        var originalCreatedOn = insertedEntity!.CreatedDate;
        
        // Wait a moment to ensure time difference
        await Task.Delay(100);

        // Act - Update the entity
        insertedEntity!.Name = "Updated Entity";
        await DbContext.AuditableEntities.UpdateAsync(insertedEntity);
        
        // Get the updated entity
        var updatedEntity = await DbContext.AuditableEntities.GetByIdAsync<int>(insertedId);

        // Assert
        Assert.NotNull(updatedEntity);
        Assert.Equal("Updated Entity", updatedEntity!.Name);
        
        // CreatedOn should not change during update
        Assert.Equal(originalCreatedOn, updatedEntity!.CreatedDate);
    }

    [Fact]
    public async Task AuditableEntityInterceptor_ShouldSetCreatedDateProperty()
    {
        // Arrange
        var entity = new EntityWithCreatedDate
        {
            Name = "Entity with CreatedDate"
            // CreatedDate is not set
        };

        // Store the current time to compare with
        var beforeInsert = DateTime.UtcNow;

        // Act
        var insertedId = await DbContext.EntitiesWithCreatedDate.InsertAsync<int>(entity);
        var retrievedEntity = await DbContext.EntitiesWithCreatedDate.GetByIdAsync<int>(insertedId);

        // Assert
        Assert.NotNull(retrievedEntity);
        Assert.Equal("Entity with CreatedDate", retrievedEntity!.Name);
        
        // CreatedDate should be set by the interceptor
        Assert.NotEqual(default, retrievedEntity!.CreatedDate);
        
        // CreatedDate should be approximately the current time
        var timeDifference = (retrievedEntity!.CreatedDate - beforeInsert).TotalSeconds;
        Assert.True(timeDifference >= 0); // CreatedDate should not be before the test started
        Assert.True(timeDifference < 5); // Should be within a few seconds
    }

    [Fact]
    public async Task CustomInterceptor_WithCustomPropertyName_ShouldSetSpecifiedProperty()
    {
        // Arrange
        var customPropertyInterceptor = new AuditableEntityInterceptor(
            dateTimeProvider: () => new DateTime(2025, 1, 1), // Fixed date for testing
            creationPropertyNames: new[] { "CustomDateField" }
        );
        
        var customSet = new DapperSet<CustomPropertyEntity>(Connection, DbContext.Transaction, customPropertyInterceptor);
        
        var entity = new CustomPropertyEntity
        {
            Name = "Custom Property Entity"
            // CustomDateField is not set
        };

        // Act
        var insertedId = await customSet.InsertAsync<int>(entity);
        
        // Use Dapper to get the property value since we don't have a DapperSet for this entity type
        var sql = "SELECT * FROM custom_property_entity WHERE id = @Id";
        var retrievedEntity = await Connection.QueryFirstOrDefaultAsync<CustomPropertyEntity>(sql, new { Id = insertedId }, DbContext.Transaction);

        // Assert
        Assert.NotNull(retrievedEntity);
        Assert.Equal("Custom Property Entity", retrievedEntity!.Name);
        Assert.Equal(new DateTime(2025, 1, 1), retrievedEntity!.CustomDateField);
    }

    [Fact]
    public async Task CustomInterceptor_ShouldBeCalledDuringInsertAndUpdate()
    {
        // Arrange
        var callLog = new CallLogInterceptor();
        var customSet = new DapperSet<AuditableEntity>(Connection, DbContext.Transaction, callLog);
        
        var entity = new AuditableEntity
        {
            Name = "Custom Interceptor Test"
        };

        // Act - Insert
        var insertedId = await customSet.InsertAsync<int>(entity);
        
        // Act - Update
        var retrievedEntity = await customSet.GetByIdAsync<int>(insertedId);
        retrievedEntity!.Name = "Updated Name";
        await customSet.UpdateAsync(retrievedEntity);

        // Assert
        Assert.Equal(1, callLog.BeforeInsertCount);
        Assert.Equal(1, callLog.AfterInsertCount);
        Assert.Equal(1, callLog.BeforeUpdateCount);
        Assert.Equal(1, callLog.AfterUpdateCount);
    }

    [Fact]
    public async Task MultipleInterceptors_ShouldAllBeCalled()
    {
        // Arrange
        var callLog1 = new CallLogInterceptor();
        var callLog2 = new CallLogInterceptor();
        var customSet = new DapperSet<AuditableEntity>(Connection, DbContext.Transaction, callLog1, callLog2);
        
        var entity = new AuditableEntity
        {
            Name = "Multiple Interceptors Test"
        };

        // Act
        var insertedId = await customSet.InsertAsync<int>(entity);

        // Assert
        Assert.Equal(1, callLog1.BeforeInsertCount);
        Assert.Equal(1, callLog1.AfterInsertCount);
        Assert.Equal(1, callLog2.BeforeInsertCount);
        Assert.Equal(1, callLog2.AfterInsertCount);
    }
    
    // Helper class for testing custom property names
    [Table("custom_property_entity")]
    public class CustomPropertyEntity
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
    
        [Column("name")]
        public string Name { get; set; } = string.Empty;
    
        [Column("custom_date_field")]
        public DateTime CustomDateField { get; set; }
    }

    // Helper class for testing interceptor calls
    private class CallLogInterceptor : SaveChangesInterceptor
    {
        public int BeforeInsertCount { get; private set; }
        public int AfterInsertCount { get; private set; }
        public int BeforeUpdateCount { get; private set; }
        public int AfterUpdateCount { get; private set; }

        public override Task BeforeInsertAsync(object entity, CancellationToken cancellationToken = default)
        {
            BeforeInsertCount++;
            return base.BeforeInsertAsync(entity, cancellationToken);
        }

        public override Task AfterInsertAsync(object entity, CancellationToken cancellationToken = default)
        {
            AfterInsertCount++;
            return base.AfterInsertAsync(entity, cancellationToken);
        }

        public override Task BeforeUpdateAsync(object entity, CancellationToken cancellationToken = default)
        {
            BeforeUpdateCount++;
            return base.BeforeUpdateAsync(entity, cancellationToken);
        }

        public override Task AfterUpdateAsync(object entity, CancellationToken cancellationToken = default)
        {
            AfterUpdateCount++;
            return base.AfterUpdateAsync(entity, cancellationToken);
        }
    }
}