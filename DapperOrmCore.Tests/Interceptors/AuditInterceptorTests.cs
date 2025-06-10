using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using DapperOrmCore.Interceptors;
using Xunit;

namespace DapperOrmCore.Tests.Interceptors;

public class AuditInterceptorTests : TestSetup
{
    [Fact]
    public async Task AuditInterceptor_ShouldUpdateLastEditDateButNotCreatedDate()
    {
        // Arrange - Create the test table
        Connection.Execute(@"
            CREATE TABLE audit_entity (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                created_date TEXT,
                last_edit_date TEXT
            )");
            
        // Create an interceptor that sets dates
        var auditInterceptor = new AuditDateInterceptor();
        
        // Create a DapperSet with the interceptor
        var auditableSet = new DapperSet<AuditEntity>(Connection, DbContext.Transaction, auditInterceptor);
        
        // Create an entity
        var entity = new AuditEntity
        {
            Name = "Initial Name"
        };

        // Act - Insert the entity
        var insertedId = await auditableSet.InsertAsync<int>(entity);
        var insertedEntity = await auditableSet.GetByIdAsync<int>(insertedId);
        
        // Store the original dates
        Assert.NotNull(insertedEntity);
        var originalCreatedDate = insertedEntity!.CreatedDate;
        var originalLastEditDate = insertedEntity.LastEditDate;
        
        // Ensure dates are set and equal after insert
        Assert.NotEqual(default, originalCreatedDate);
        Assert.Equal(originalCreatedDate, originalLastEditDate);
        
        // Wait a moment to ensure time difference
        await Task.Delay(100);
        
        // Act - Update the entity
        insertedEntity.Name = "Updated Name";
        await auditableSet.UpdateAsync(insertedEntity);
        
        // Get the updated entity
        var updatedEntity = await auditableSet.GetByIdAsync<int>(insertedId);
        
        // Assert
        Assert.NotNull(updatedEntity);
        Assert.Equal("Updated Name", updatedEntity!.Name);
        
        // CreatedDate should not change during update
        Assert.Equal(originalCreatedDate, updatedEntity.CreatedDate);
        
        // LastEditDate should be updated
        Assert.NotEqual(originalLastEditDate, updatedEntity.LastEditDate);
        Assert.True(updatedEntity.LastEditDate > originalLastEditDate);
    }
    
    [Fact]
    public async Task LastEditUserInterceptor_ShouldTrackLastEditUser()
    {
        // Arrange - Create the test table
        Connection.Execute(@"
            CREATE TABLE user_edit_entity (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                created_by TEXT,
                last_edited_by TEXT
            )");
            
        // Create an interceptor that tracks users
        var userInterceptor = new LastEditUserInterceptor("User1");
        
        // Create a DapperSet with the interceptor
        var userTrackingSet = new DapperSet<UserEditEntity>(Connection, DbContext.Transaction, userInterceptor);
        
        // Create an entity
        var entity = new UserEditEntity
        {
            Name = "Initial Record"
        };
        
        // Act - Insert as User1
        var insertedId = await userTrackingSet.InsertAsync<int>(entity);
        var insertedEntity = await userTrackingSet.GetByIdAsync<int>(insertedId);
        
        // Assert after insert
        Assert.NotNull(insertedEntity);
        Assert.Equal("User1", insertedEntity!.CreatedBy);
        Assert.Equal("User1", insertedEntity.LastEditedBy);
        
        // Change the current user
        userInterceptor.CurrentUser = "User2";
        
        // Act - Update as User2
        insertedEntity.Name = "Updated Record";
        await userTrackingSet.UpdateAsync(insertedEntity);
        
        // Get the updated entity
        var updatedEntity = await userTrackingSet.GetByIdAsync<int>(insertedId);
        
        // Assert after update
        Assert.NotNull(updatedEntity);
        Assert.Equal("Updated Record", updatedEntity!.Name);
        
        // CreatedBy should not change
        Assert.Equal("User1", updatedEntity.CreatedBy);
        
        // LastEditedBy should be updated to User2
        Assert.Equal("User2", updatedEntity.LastEditedBy);
    }

    // Test models and interceptors

    // Audit entity model
    [Table("audit_entity")]
    public class AuditEntity
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("created_date")]
        public DateTime CreatedDate { get; set; }

        [Column("last_edit_date")]
        public DateTime LastEditDate { get; set; }
    }

    // Audit date interceptor
    private class AuditDateInterceptor : SaveChangesInterceptor
    {
        public override Task BeforeInsertAsync(object entity, CancellationToken cancellationToken = default)
        {
            if (entity is AuditEntity auditEntity)
            {
                var now = DateTime.UtcNow;
                auditEntity.CreatedDate = now;
                auditEntity.LastEditDate = now;
            }
            
            return Task.CompletedTask;
        }
        
        public override Task BeforeUpdateAsync(object entity, CancellationToken cancellationToken = default)
        {
            if (entity is AuditEntity auditEntity)
            {
                // Only update LastEditDate, not CreatedDate
                auditEntity.LastEditDate = DateTime.UtcNow;
            }
            
            return Task.CompletedTask;
        }
    }

    // User edit entity model
    [Table("user_edit_entity")]
    public class UserEditEntity
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("created_by")]
        public string? CreatedBy { get; set; }

        [Column("last_edited_by")]
        public string? LastEditedBy { get; set; }
    }

    // Last edit user interceptor
    private class LastEditUserInterceptor : SaveChangesInterceptor
    {
        public string CurrentUser { get; set; }

        public LastEditUserInterceptor(string initialUser)
        {
            CurrentUser = initialUser;
        }

        public override Task BeforeInsertAsync(object entity, CancellationToken cancellationToken = default)
        {
            if (entity is UserEditEntity userEntity)
            {
                userEntity.CreatedBy = CurrentUser;
                userEntity.LastEditedBy = CurrentUser;
            }
            
            return Task.CompletedTask;
        }
        
        public override Task BeforeUpdateAsync(object entity, CancellationToken cancellationToken = default)
        {
            if (entity is UserEditEntity userEntity)
            {
                // Only update LastEditedBy, not CreatedBy
                userEntity.LastEditedBy = CurrentUser;
            }
            
            return Task.CompletedTask;
        }
    }
}