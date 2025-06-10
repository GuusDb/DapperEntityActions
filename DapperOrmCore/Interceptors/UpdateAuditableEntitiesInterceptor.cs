using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DapperOrmCore.Interceptors;

/// <summary>
/// A flexible interceptor that sets creation date/time properties on entities when they are inserted.
/// </summary>
public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly string[] _creationPropertyNames;
    private readonly Func<DateTime> _dateTimeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditableEntityInterceptor"/> class.
    /// </summary>
    /// <param name="dateTimeProvider">A function that provides the current date and time. Defaults to DateTime.UtcNow.</param>
    /// <param name="creationPropertyNames">The names of properties to look for and set with the current date/time. Defaults to common naming conventions.</param>
    public AuditableEntityInterceptor(
        Func<DateTime>? dateTimeProvider = null,
        string[]? creationPropertyNames = null)
    {
        _dateTimeProvider = dateTimeProvider ?? (() => DateTime.UtcNow);
        _creationPropertyNames = creationPropertyNames ?? new[]
        {
            "CreatedOn",
            "CreatedAt",
            "CreatedDate",
            "DateCreated",
            "CreateDate",
            "CreationDate",
            "CreationTime"
        };
    }

    /// <inheritdoc />
    public override Task BeforeInsertAsync(object entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            return Task.CompletedTask;

        var entityType = entity.GetType();
        var currentDateTime = _dateTimeProvider();

        foreach (var propertyName in _creationPropertyNames)
        {
            var property = entityType.GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.Instance);

            if (property != null && property.CanWrite)
            {
                if (property.PropertyType == typeof(DateTime) ||
                    property.PropertyType == typeof(DateTime?))
                {
                    var currentValue = property.GetValue(entity);
                    if (currentValue == null ||
                        (property.PropertyType == typeof(DateTime) && (DateTime)currentValue == default))
                    {
                        property.SetValue(entity, currentDateTime);
                        break; 
                    }
                }
            }
        }

        return Task.CompletedTask;
    }
}