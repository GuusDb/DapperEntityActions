using Dapper;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace DapperOrmCore;
public class DapperQuery<T> where T : class
{
    private readonly DapperSet<T> _parent;
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;
    private readonly string _fullTableName;
    private readonly Dictionary<string, PropertyInfo> _propertyMap;
    private string _whereClause = string.Empty;
    private DynamicParameters _parameters = new DynamicParameters();
    private string _orderByClause = string.Empty;

    public DapperQuery(DapperSet<T> parent, IDbConnection connection, IDbTransaction transaction,
        string fullTableName, Dictionary<string, PropertyInfo> propertyMap)
    {
        _parent = parent;
        _connection = connection;
        _transaction = transaction;
        _fullTableName = fullTableName;
        _propertyMap = propertyMap;
    }

    public DapperQuery<T> Where(Expression<Func<T, bool>> predicate)
    {
        var visitor = new WhereExpressionVisitor<T>(_propertyMap);
        var (sqlCondition, parameters) = visitor.Translate(predicate);
        _whereClause = sqlCondition;
        _parameters = parameters;
        return this;
    }

    public DapperQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> orderByExpression, bool descending = false)
    {
        var memberExpression = orderByExpression.Body as MemberExpression
            ?? (orderByExpression.Body as UnaryExpression)?.Operand as MemberExpression;

        if (memberExpression == null || memberExpression.Expression?.Type != typeof(T))
        {
            throw new ArgumentException("Invalid order by expression.");
        }

        string propertyName = memberExpression.Member.Name;
        string columnName = _propertyMap.Keys.FirstOrDefault(k =>
            string.Equals(k, propertyName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(_propertyMap[k].Name, propertyName, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Property '{propertyName}' not found in entity.");

        string orderDirection = descending ? "DESC" : "ASC";
        _orderByClause = $"ORDER BY {columnName} {orderDirection}";
        return this;
    }

    public async Task<IEnumerable<T>> ExecuteAsync()
    {
        string sql = $"SELECT * FROM {_fullTableName}";
        if (!string.IsNullOrEmpty(_whereClause))
        {
            sql += $" WHERE {_whereClause}";
        }
        if (!string.IsNullOrEmpty(_orderByClause))
        {
            sql += $" {_orderByClause}";
        }

        return await _connection.QueryAsync<T>(sql, _parameters, transaction: _transaction);
    }
}