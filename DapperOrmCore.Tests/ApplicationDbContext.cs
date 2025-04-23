using DapperOrmCore;
using DapperOrmCore.Tests.Models;
using System.Data;

namespace DapperOrmCore.Tests;

public class ApplicationDbContext : IDisposable
{
    private readonly IDbConnection _connection;
    private IDbTransaction _transaction;

    public DapperSet<Plant> Plants { get; }
    public DapperSet<TestLalala> Tests { get; }
    public DapperSet<CoolMeasurement> Measurements { get; }

    public ApplicationDbContext(IDbConnection connection)
    {
        _connection = connection;
        _connection.Open();
        _transaction = _connection.BeginTransaction();
        Plants = new DapperSet<Plant>(_connection, _transaction);
        Tests = new DapperSet<TestLalala>(_connection, _transaction);
        Measurements = new DapperSet<CoolMeasurement>(_connection, _transaction);
    }

    public void Commit() => _transaction?.Commit();
    public void Rollback() => _transaction?.Rollback();

    public void Dispose()
    {
        _transaction?.Dispose();
        _connection?.Dispose();
    }
}