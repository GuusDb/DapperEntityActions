using Dapper;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace DapperOrmCore;

public class DapperQuery<T> where T : class
{
    private readonly DapperSet<T> _parent;
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;
    private readonly string _fullTableName;
    private readonly Dictionary<string, PropertyInfo> _propertyMap;
    private readonly StringBuilder _whereClause = new StringBuilder();
    private DynamicParameters _parameters = new DynamicParameters();
    private readonly StringBuilder _orderByClause = new StringBuilder();
    private int _paramCounter = 0;

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

        if (_whereClause.Length > 0)
        {
            _whereClause.Append(" AND ");
        }

        var paramMapping = new Dictionary<string, string>();
        string newSqlCondition = sqlCondition;
        foreach (var paramName in parameters.ParameterNames)
        {
            // Strip the @ from the original paramName if present, since we'll add it once
            string baseParamName = paramName.StartsWith("@") ? paramName.Substring(1) : paramName;
            string newParamName = $"p{_paramCounter++}";  // No @ here, we'll add it in the SQL
            paramMapping[baseParamName] = newParamName;
            newSqlCondition = newSqlCondition.Replace($"@{baseParamName}", $"@{newParamName}");
            _parameters.Add(newParamName, parameters.Get<object>(paramName));
        }

        _whereClause.Append(newSqlCondition);

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

        if (_orderByClause.Length > 0)
        {
            _orderByClause.Append(", ");
        }
        else
        {
            _orderByClause.Append("ORDER BY ");
        }
        _orderByClause.Append($"{columnName} {orderDirection}");

        return this;
    }

    public async Task<IEnumerable<T>> ExecuteAsync()
    {
        string sql = $"SELECT * FROM {_fullTableName}";
        if (_whereClause.Length > 0)
        {
            sql += $" WHERE {_whereClause}";
        }
        if (_orderByClause.Length > 0)
        {
            sql += $" {_orderByClause}";
        }

        Console.WriteLine($"Executing SQL: {sql}");
        Console.WriteLine($"Parameters: {string.Join(", ", _parameters.ParameterNames.Select(n => $"{n}={_parameters.Get<object>(n)}"))}");

        return await _connection.QueryAsync<T>(sql, _parameters, transaction: _transaction);
    }
}