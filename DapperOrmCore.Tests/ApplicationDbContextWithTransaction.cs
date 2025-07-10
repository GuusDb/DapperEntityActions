using DapperOrmCore;
using DapperOrmCore.Interceptors;
using DapperOrmCore.Tests.Models;
using System.Data;

namespace DapperOrmCore.Tests;

/// <summary>
/// A version of ApplicationDbContext that exposes methods to create and manage transactions
/// for testing transaction-related functionality.
/// </summary>
public class ApplicationDbContextWithTransaction : IDisposable
{
    private readonly IDbConnection _connection;
    private IDbTransaction _transaction;
    
    /// <summary>
    /// Gets the current transaction. Exposed for testing purposes.
    /// </summary>
    public IDbTransaction Transaction => _transaction;

    public DapperSet<Plant> Plants { get; }
    public DapperSet<TestLalala> Tests { get; }
    public DapperSet<CoolMeasurement> Measurements { get; }
    public DapperSet<Parent> Parents { get; }
    public DapperSet<Child> Children { get; }
    public DapperSet<AuditableEntity> AuditableEntities { get; }
    public DapperSet<EntityWithCreatedDate> EntitiesWithCreatedDate { get; }

    public ApplicationDbContextWithTransaction(IDbConnection connection)
    {
        _connection = connection;
        _connection.Open();
        
        // Create an interceptor for auditable entities
        var auditInterceptor = new AuditableEntityInterceptor();
        
        // Initialize DapperSets without transaction
        Plants = new DapperSet<Plant>(_connection);
        Tests = new DapperSet<TestLalala>(_connection);
        Measurements = new DapperSet<CoolMeasurement>(_connection);
        Parents = new DapperSet<Parent>(_connection);
        Children = new DapperSet<Child>(_connection);
        
        // Use the interceptor for AuditableEntities and EntitiesWithCreatedDate
        AuditableEntities = new DapperSet<AuditableEntity>(_connection, null, auditInterceptor);
        EntitiesWithCreatedDate = new DapperSet<EntityWithCreatedDate>(_connection, null, auditInterceptor);
    }

    /// <summary>
    /// Begins a new transaction.
    /// </summary>
    /// <returns>The newly created transaction.</returns>
    public IDbTransaction BeginTransaction()
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }
        
        _transaction = _connection.BeginTransaction();
        return _transaction;
    }

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    public void Commit()
    {
        _transaction?.Commit();
        _transaction?.Dispose();
        _transaction = null;
    }

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    public void Rollback()
    {
        _transaction?.Rollback();
        _transaction?.Dispose();
        _transaction = null;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _connection?.Dispose();
    }
}