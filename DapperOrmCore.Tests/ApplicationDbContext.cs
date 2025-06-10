using DapperOrmCore;
using DapperOrmCore.Interceptors;
using DapperOrmCore.Tests.Models;
using System.Data;

namespace DapperOrmCore.Tests;

public class ApplicationDbContext : IDisposable
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

    public ApplicationDbContext(IDbConnection connection)
    {
        _connection = connection;
        _connection.Open();
        _transaction = _connection.BeginTransaction();
        
        // Create an interceptor for auditable entities
        var auditInterceptor = new AuditableEntityInterceptor();
        
        Plants = new DapperSet<Plant>(_connection, _transaction);
        Tests = new DapperSet<TestLalala>(_connection, _transaction);
        Measurements = new DapperSet<CoolMeasurement>(_connection, _transaction);
        Parents = new DapperSet<Parent>(_connection, _transaction);
        Children = new DapperSet<Child>(_connection, _transaction);
        
        // Use the interceptor for AuditableEntities and EntitiesWithCreatedDate
        AuditableEntities = new DapperSet<AuditableEntity>(_connection, _transaction, auditInterceptor);
        EntitiesWithCreatedDate = new DapperSet<EntityWithCreatedDate>(_connection, _transaction, auditInterceptor);
    }

    public void Commit() => _transaction?.Commit();
    public void Rollback() => _transaction?.Rollback();

    public void Dispose()
    {
        _transaction?.Dispose();
        _connection?.Dispose();
    }
}