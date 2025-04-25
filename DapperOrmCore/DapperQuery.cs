using Dapper;
using DapperOrmCore;
using DapperOrmCore.Models;
using DapperOrmCore.Visitors;
using Serilog;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;


public class DapperQuery<T> where T : class
{
    private readonly DapperSet<T> _parent;
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;
    private readonly string _fullTableName;
    private readonly Dictionary<string, PropertyInfo> _propertyMap;
    private readonly Dictionary<string, NavigationPropertyInfo> _navigationProperties;
    private readonly List<string> _whereClauses = new List<string>();
    private DynamicParameters _parameters = new DynamicParameters();
    private readonly StringBuilder _orderByClause = new StringBuilder();
    private readonly List<string> _includedProperties = new List<string>();
    private readonly List<string> _referencedNavProps = new List<string>();
    private int _paramCounter = 0;
    private int? _pageIndex;
    private int? _pageSize;

    public DapperQuery(DapperSet<T> parent, IDbConnection connection, IDbTransaction transaction,
        string fullTableName, Dictionary<string, PropertyInfo> propertyMap,
        Dictionary<string, NavigationPropertyInfo> navigationProperties)
    {
        _parent = parent;
        _connection = connection;
        _transaction = transaction;
        _fullTableName = fullTableName;
        _propertyMap = propertyMap;
        _navigationProperties = navigationProperties;
    }

    public DapperQuery<T> Where(Expression<Func<T, bool>> predicate)
    {
        // Extract navigation properties from the predicate
        var navProps = new NavigationPropertyExtractor(_navigationProperties.Keys.ToList())
            .Extract(predicate);
        foreach (var navProp in navProps)
        {
            if (!_referencedNavProps.Contains(navProp))
                _referencedNavProps.Add(navProp);
        }

        var visitor = new WhereExpressionVisitor<T>(_propertyMap, _navigationProperties, _referencedNavProps);
        var (sqlCondition, parameters) = visitor.Translate(predicate);

        var paramMapping = new Dictionary<string, string>();
        string newSqlCondition = sqlCondition;
        foreach (var paramName in parameters.ParameterNames)
        {
            string baseParamName = paramName.StartsWith("@") ? paramName.Substring(1) : paramName;
            string newParamName = $"p{_paramCounter++}";
            paramMapping[baseParamName] = newParamName;
            newSqlCondition = newSqlCondition.Replace($"@{baseParamName}", $"@{newParamName}");
            _parameters.Add(newParamName, parameters.Get<object>(paramName));
        }

        _whereClauses.Add(newSqlCondition);

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

    public DapperQuery<T> Include<TProperty>(Expression<Func<T, TProperty>> navigationProperty)
    {
        var memberExpression = navigationProperty.Body as MemberExpression;
        if (memberExpression == null || memberExpression.Expression?.Type != typeof(T))
        {
            throw new ArgumentException("Invalid navigation property expression.");
        }

        string propertyName = memberExpression.Member.Name;
        if (!_navigationProperties.ContainsKey(propertyName))
        {
            throw new ArgumentException($"Navigation property '{propertyName}' not found.");
        }

        if (!_includedProperties.Contains(propertyName))
        {
            _includedProperties.Add(propertyName);
        }
        return this;
    }

    public DapperQuery<T> Paginate(int pageIndex, int pageSize)
    {
        if (pageIndex < 0)
        {
            throw new ArgumentException("Page index cannot be negative.");
        }
        if (pageSize <= 0)
        {
            throw new ArgumentException("Page size must be greater than zero.");
        }

        _pageIndex = pageIndex;
        _pageSize = pageSize;
        return this;
    }

    public async Task<IEnumerable<T>> ExecuteAsync()
    {
        var sqlBuilder = new StringBuilder();
        var selectColumns = new List<string> { $"t.*" };
        var joins = new List<string>();
        var splitOn = new List<string> { "id" };
        var types = new List<Type> { typeof(T) };
        var mappings = new List<Func<object[], T>>();

        // Combine included and referenced navigation properties
        var allNavProps = _includedProperties.Union(_referencedNavProps).Distinct().ToList();

        // Handle navigation properties
        for (int i = 0; i < allNavProps.Count; i++)
        {
            var navProp = _navigationProperties[allNavProps[i]];
            string alias = $"r{i + 1}";
            string relatedTable = navProp.RelatedTableName;
            string fkColumn = navProp.ForeignKeyColumn;

            string relatedPkColumn = navProp.RelatedType.GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null)
                ?.GetCustomAttribute<ColumnAttribute>()?.Name
                ?? throw new InvalidOperationException($"Primary key not found for {navProp.RelatedType.Name}");

            joins.Add($"LEFT JOIN {relatedTable} {alias} ON t.{fkColumn} = {alias}.{relatedPkColumn}");

            if (_includedProperties.Contains(allNavProps[i]))
            {
                selectColumns.Add($"{alias}.*");
                splitOn.Add(relatedPkColumn);
                types.Add(navProp.RelatedType);

                int index = i + 1;
                mappings.Add(objects =>
                {
                    var entity = (T)objects[0];
                    if (objects[index] != null)
                    {
                        navProp.Property.SetValue(entity, objects[index]);
                    }
                    return entity;
                });
            }
        }

        // Build the final SQL query
        sqlBuilder.Append($"SELECT {string.Join(", ", selectColumns)}");
        sqlBuilder.Append($" FROM {_fullTableName} t");
        if (joins.Any())
        {
            sqlBuilder.Append(" " + string.Join(" ", joins));
        }

        if (_whereClauses.Any())
        {
            sqlBuilder.Append($" WHERE {string.Join(" AND ", _whereClauses)}");
        }

        if (_orderByClause.Length > 0)
        {
            sqlBuilder.Append($" {_orderByClause}");
        }

        if (_pageIndex.HasValue && _pageSize.HasValue)
        {
            int offset = _pageIndex.Value * _pageSize.Value;
            sqlBuilder.Append($" LIMIT {_pageSize.Value} OFFSET {offset}");
        }

        string sql = sqlBuilder.ToString();
        Log.Information($"Executing SQL: {sql}");
        Log.Information($"Parameters: {string.Join(", ", _parameters.ParameterNames.Select(n => $"{n}={_parameters.Get<object>(n)}"))}");

        if (_includedProperties.Count == 0)
        {
            return await _connection.QueryAsync<T>(sql, _parameters, transaction: _transaction);
        }
        else if (_includedProperties.Count == 1)
        {
            var results = await _connection.QueryAsync(
                sql,
                types.ToArray(),
                objects =>
                {
                    var entity = (T)objects[0];
                    if (objects[1] != null)
                    {
                        _navigationProperties[_includedProperties[0]].Property.SetValue(entity, objects[1]);
                    }
                    return entity;
                },
                _parameters,
                transaction: _transaction,
                splitOn: string.Join(",", splitOn)
            );
            return results;
        }
        else
        {
            var lookup = new Dictionary<object, T>();
            var results = await _connection.QueryAsync(
                sql,
                types.ToArray(),
                objects =>
                {
                    var entity = (T)objects[0];
                    var pkValue = _propertyMap.First(p => p.Value.GetCustomAttribute<KeyAttribute>() != null).Value.GetValue(entity);
                    if (!lookup.TryGetValue(pkValue, out T existing))
                    {
                        lookup[pkValue] = entity;
                        existing = entity;
                    }
                    for (int i = 0; i < _includedProperties.Count; i++)
                    {
                        if (objects[i + 1] != null)
                        {
                            _navigationProperties[_includedProperties[i]].Property.SetValue(existing, objects[i + 1]);
                        }
                    }
                    return existing;
                },
                _parameters,
                transaction: _transaction,
                splitOn: string.Join(",", splitOn)
            );
            return lookup.Values;
        }
    }
}

