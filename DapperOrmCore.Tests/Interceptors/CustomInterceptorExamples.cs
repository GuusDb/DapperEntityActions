using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DapperOrmCore.Interceptors;

namespace DapperOrmCore.Tests.Interceptors;

/// <summary>
/// Example of a custom interceptor that sets a LastModifiedOn property on entities when they are updated.
/// </summary>
public class LastModifiedInterceptor : SaveChangesInterceptor
{
    private readonly string[] _modificationPropertyNames;
    private readonly Func<DateTime> _dateTimeProvider;

    public LastModifiedInterceptor(
        Func<DateTime>? dateTimeProvider = null,
        string[]? modificationPropertyNames = null)
    {
        _dateTimeProvider = dateTimeProvider ?? (() => DateTime.UtcNow);
        _modificationPropertyNames = modificationPropertyNames ?? new[]
        {
            "LastModifiedOn",
            "LastModifiedAt",
            "LastModifiedDate",
            "DateModified",
            "ModifiedDate",
            "UpdatedOn",
            "UpdatedAt",
            "UpdateDate"
        };
    }

    public override Task BeforeUpdateAsync(object entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            return Task.CompletedTask;

        var entityType = entity.GetType();
        var currentDateTime = _dateTimeProvider();

        // Look for any property that matches our modification property names
        foreach (var propertyName in _modificationPropertyNames)
        {
            var property = entityType.GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.Instance);

            if (property != null && property.CanWrite)
            {
                // Check if the property type is compatible with DateTime
                if (property.PropertyType == typeof(DateTime) ||
                    property.PropertyType == typeof(DateTime?))
                {
                    property.SetValue(entity, currentDateTime);
                    break; // Set only the first matching property
                }
            }
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Example of a custom interceptor that sets a soft delete flag instead of actually deleting records.
/// </summary>
public class SoftDeleteInterceptor : SaveChangesInterceptor
{
    private readonly string[] _deletedPropertyNames;

    public SoftDeleteInterceptor(string[]? deletedPropertyNames = null)
    {
        _deletedPropertyNames = deletedPropertyNames ?? new[]
        {
            "IsDeleted",
            "Deleted",
            "IsActive"
        };
    }

    // This would be called before a delete operation if we had a BeforeDeleteAsync method
    // For now, it's just an example of what could be done
    public void MarkAsDeleted(object entity)
    {
        if (entity == null)
            return;

        var entityType = entity.GetType();

        // Look for any property that matches our deleted property names
        foreach (var propertyName in _deletedPropertyNames)
        {
            var property = entityType.GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.Instance);

            if (property != null && property.CanWrite)
            {
                // Handle different property types
                if (property.PropertyType == typeof(bool))
                {
                    // For IsDeleted, set to true; for IsActive, set to false
                    bool value = propertyName.ToLower().Contains("active") ? false : true;
                    property.SetValue(entity, value);
                    break;
                }
                else if (property.PropertyType == typeof(DateTime) ||
                         property.PropertyType == typeof(DateTime?))
                {
                    property.SetValue(entity, DateTime.UtcNow);
                    break;
                }
            }
        }
    }
}

/// <summary>
/// Example of a custom interceptor that validates entities before they are inserted or updated.
/// </summary>
public class ValidationInterceptor : SaveChangesInterceptor
{
    public override Task BeforeInsertAsync(object entity, CancellationToken cancellationToken = default)
    {
        ValidateEntity(entity);
        return Task.CompletedTask;
    }

    public override Task BeforeUpdateAsync(object entity, CancellationToken cancellationToken = default)
    {
        ValidateEntity(entity);
        return Task.CompletedTask;
    }

    private void ValidateEntity(object entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity), "Entity cannot be null");

        var entityType = entity.GetType();
        
        // Example validation: Check if required string properties have values
        foreach (var property in entityType.GetProperties())
        {
            if (property.PropertyType == typeof(string) && 
                property.GetCustomAttribute<RequiredAttribute>() != null)
            {
                var value = property.GetValue(entity) as string;
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ValidationException($"Property {property.Name} is required but has no value");
                }
            }
        }
    }
}