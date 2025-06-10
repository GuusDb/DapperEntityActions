using System.Threading;
using System.Threading.Tasks;

namespace DapperOrmCore.Interceptors;

/// <summary>
/// Base implementation of <see cref="ISaveChangesInterceptor"/> that provides empty implementations for all methods.
/// </summary>
public abstract class SaveChangesInterceptor : ISaveChangesInterceptor
{
    /// <inheritdoc />
    public virtual Task BeforeInsertAsync(object entity, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task AfterInsertAsync(object entity, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task BeforeUpdateAsync(object entity, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task AfterUpdateAsync(object entity, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}