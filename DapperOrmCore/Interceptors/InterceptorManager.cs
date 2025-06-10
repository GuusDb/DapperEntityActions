using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DapperOrmCore.Interceptors;

/// <summary>
/// Manages the interceptors for the DapperOrmCore.
/// </summary>
public class InterceptorManager
{
    private readonly List<ISaveChangesInterceptor> _interceptors = new List<ISaveChangesInterceptor>();

    /// <summary>
    /// Adds an interceptor to the manager.
    /// </summary>
    /// <param name="interceptor">The interceptor to add.</param>
    public void AddInterceptor(ISaveChangesInterceptor interceptor)
    {
        _interceptors.Add(interceptor);
    }

    /// <summary>
    /// Calls the BeforeInsertAsync method on all interceptors.
    /// </summary>
    /// <param name="entity">The entity being inserted.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task BeforeInsertAsync(object entity, CancellationToken cancellationToken = default)
    {
        foreach (var interceptor in _interceptors)
        {
            await interceptor.BeforeInsertAsync(entity, cancellationToken);
        }
    }

    /// <summary>
    /// Calls the AfterInsertAsync method on all interceptors.
    /// </summary>
    /// <param name="entity">The entity that was inserted.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AfterInsertAsync(object entity, CancellationToken cancellationToken = default)
    {
        foreach (var interceptor in _interceptors)
        {
            await interceptor.AfterInsertAsync(entity, cancellationToken);
        }
    }

    /// <summary>
    /// Calls the BeforeUpdateAsync method on all interceptors.
    /// </summary>
    /// <param name="entity">The entity being updated.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task BeforeUpdateAsync(object entity, CancellationToken cancellationToken = default)
    {
        foreach (var interceptor in _interceptors)
        {
            await interceptor.BeforeUpdateAsync(entity, cancellationToken);
        }
    }

    /// <summary>
    /// Calls the AfterUpdateAsync method on all interceptors.
    /// </summary>
    /// <param name="entity">The entity that was updated.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AfterUpdateAsync(object entity, CancellationToken cancellationToken = default)
    {
        foreach (var interceptor in _interceptors)
        {
            await interceptor.AfterUpdateAsync(entity, cancellationToken);
        }
    }
}