using System.Threading;
using System.Threading.Tasks;

namespace DapperOrmCore.Interceptors;

/// <summary>
/// Interface for interceptors that can modify entities before or after they are saved to the database.
/// </summary>
public interface ISaveChangesInterceptor
{
    /// <summary>
    /// Called before an entity is inserted into the database.
    /// </summary>
    /// <param name="entity">The entity being inserted.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task BeforeInsertAsync(object entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called after an entity is inserted into the database.
    /// </summary>
    /// <param name="entity">The entity that was inserted.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AfterInsertAsync(object entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called before an entity is updated in the database.
    /// </summary>
    /// <param name="entity">The entity being updated.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task BeforeUpdateAsync(object entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called after an entity is updated in the database.
    /// </summary>
    /// <param name="entity">The entity that was updated.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AfterUpdateAsync(object entity, CancellationToken cancellationToken = default);
}