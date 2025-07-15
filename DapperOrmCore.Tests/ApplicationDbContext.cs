using DapperOrmCore;
using DapperOrmCore.Interceptors;
using DapperOrmCore.Tests.Models;
using System.Data;

namespace DapperOrmCore.Tests;

public class ApplicationDbContext : IDisposable
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
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    /// <param name="connection">The database connection to use for operations.</param>
    public ApplicationDbContext(IDbConnection connection)
        : this(connection, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class with a specific database provider.
    /// </summary>
    /// <param name="connection">The database connection to use for operations.</param>
    /// <param name="databaseProvider">The database provider to use for generating SQL syntax. If null, SQLite will be used as the default.</param>
    public ApplicationDbContext(IDbConnection connection, DatabaseProvider? databaseProvider = null)
    {
        _connection = connection;
        _databaseProvider = databaseProvider ?? DatabaseProvider.SQLite;
        _connection.Open();
        _transaction = _connection.BeginTransaction();
        
        // Create an interceptor for auditable entities
        var auditInterceptor = new AuditableEntityInterceptor();
        
        Plants = new DapperSet<Plant>(_connection, _databaseProvider, _transaction);
        Tests = new DapperSet<TestLalala>(_connection, _databaseProvider, _transaction);
        Measurements = new DapperSet<CoolMeasurement>(_connection, _databaseProvider, _transaction);
        Parents = new DapperSet<Parent>(_connection, _databaseProvider, _transaction);
        Children = new DapperSet<Child>(_connection, _databaseProvider, _transaction);
        
        // Use the interceptor for AuditableEntities and EntitiesWithCreatedDate
        AuditableEntities = new DapperSet<AuditableEntity>(_connection, _databaseProvider, _transaction, auditInterceptor);
        EntitiesWithCreatedDate = new DapperSet<EntityWithCreatedDate>(_connection, _databaseProvider, _transaction, auditInterceptor);
    }

    public void Commit() => _transaction?.Commit();
    public void Rollback() => _transaction?.Rollback();

    public void Dispose()
    {
        _transaction?.Dispose();
        _connection?.Dispose();
    }
}