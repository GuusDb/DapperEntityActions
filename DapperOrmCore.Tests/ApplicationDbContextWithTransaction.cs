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
    private readonly DatabaseProvider? _databaseProvider;
    
    /// <summary>
    /// Gets the current transaction. Exposed for testing purposes.
    /// </summary>
    public IDbTransaction Transaction => _transaction;
    
    /// <summary>
    /// Gets the database provider used by this context.
    /// </summary>
    public DatabaseProvider DatabaseProvider => _databaseProvider ?? DatabaseProvider.SQLite;

    public DapperSet<Plant> Plants { get; }
    public DapperSet<TestLalala> Tests { get; }
    public DapperSet<CoolMeasurement> Measurements { get; }
    public DapperSet<Parent> Parents { get; }
    public DapperSet<Child> Children { get; }
    public DapperSet<AuditableEntity> AuditableEntities { get; }
    public DapperSet<EntityWithCreatedDate> EntitiesWithCreatedDate { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContextWithTransaction"/> class.
    /// </summary>
    /// <param name="connection">The database connection to use for operations.</param>
    public ApplicationDbContextWithTransaction(IDbConnection connection)
        : this(connection, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContextWithTransaction"/> class with a specific database provider.
    /// </summary>
    /// <param name="connection">The database connection to use for operations.</param>
    /// <param name="databaseProvider">The database provider to use for generating SQL syntax. If null, SQLite will be used as the default.</param>
    public ApplicationDbContextWithTransaction(IDbConnection connection, DatabaseProvider? databaseProvider = null)
    {
        _connection = connection;
        _databaseProvider = databaseProvider ?? DatabaseProvider.SQLite;
        _connection.Open();
        
        // Create an interceptor for auditable entities
        var auditInterceptor = new AuditableEntityInterceptor();
        
        // Initialize DapperSets without transaction
        Plants = new DapperSet<Plant>(_connection, _databaseProvider);
        Tests = new DapperSet<TestLalala>(_connection, _databaseProvider);
        Measurements = new DapperSet<CoolMeasurement>(_connection, _databaseProvider);
        Parents = new DapperSet<Parent>(_connection, _databaseProvider);
        Children = new DapperSet<Child>(_connection, _databaseProvider);
        
        // Use the interceptor for AuditableEntities and EntitiesWithCreatedDate
        AuditableEntities = new DapperSet<AuditableEntity>(_connection, _databaseProvider, null, auditInterceptor);
        EntitiesWithCreatedDate = new DapperSet<EntityWithCreatedDate>(_connection, _databaseProvider, null, auditInterceptor);
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