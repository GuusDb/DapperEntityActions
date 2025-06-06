using Dapper;
using DapperOrmCore.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace DapperOrmCore;

/// <summary>
/// A generic class for performing projection queries on a database table using Dapper.
/// </summary>
/// <typeparam name="T">The entity type representing the database table.</typeparam>
/// <typeparam name="TResult">The type of the result after projection.</typeparam>
public class DapperProjectionQuery<T, TResult> where T : class
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
    private int? _pageIndex;
    private int? _pageSize;
    private readonly Expression<Func<T, TResult>> _selector;
    private readonly Func<T, TResult> _compiledSelector;
    private readonly List<string> _selectedColumns = new List<string>();

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperProjectionQuery{T, TResult}"/> class.
    /// </summary>
    public DapperProjectionQuery(
        DapperSet<T> parent,
        IDbConnection connection,
        IDbTransaction transaction,
        string fullTableName,
        Dictionary<string, PropertyInfo> propertyMap,
        Dictionary<string, NavigationPropertyInfo> navigationProperties,
        List<string> whereClauses,
        DynamicParameters parameters,
        StringBuilder orderByClause,
        List<string> includedProperties,
        List<string> referencedNavProps,
        int? pageIndex,
        int? pageSize,
        Expression<Func<T, TResult>> selector,
        List<string> selectedColumns)
    {
        _parent = parent;
        _connection = connection;
        _transaction = transaction;
        _fullTableName = fullTableName;
        _propertyMap = propertyMap;
        _navigationProperties = navigationProperties;
        _whereClauses = whereClauses;
        _parameters = parameters;
        _orderByClause = orderByClause;
        _includedProperties = includedProperties;
        _referencedNavProps = referencedNavProps;
        _pageIndex = pageIndex;
        _pageSize = pageSize;
        _selector = selector;
        _compiledSelector = selector.Compile();
        _selectedColumns = selectedColumns;
    }

    /// <summary>
    /// Filters the query results based on a predicate.
    /// </summary>
    /// <param name="predicate">An expression specifying the filter condition.</param>
    /// <returns>The current <see cref="DapperProjectionQuery{T, TResult}"/> instance for method chaining.</returns>
    public DapperProjectionQuery<T, TResult> Where(Expression<Func<T, bool>> predicate)
    {
        // Extract navigation properties from the predicate
        var navProps = new Visitors.NavigationPropertyExtractor(_navigationProperties.Keys.ToList())
            .Extract(predicate);
        foreach (var navProp in navProps)
        {
            if (!_referencedNavProps.Contains(navProp))
                _referencedNavProps.Add(navProp);
        }

        var visitor = new Visitors.WhereExpressionVisitor<T>(_propertyMap, _navigationProperties, _referencedNavProps);
        var (sqlCondition, parameters) = visitor.Translate(predicate);

        var paramCounter = _parameters.ParameterNames.Count();
        var paramMapping = new Dictionary<string, string>();
        string newSqlCondition = sqlCondition;
        foreach (var paramName in parameters.ParameterNames)
        {
            string baseParamName = paramName.StartsWith("@") ? paramName.Substring(1) : paramName;
            string newParamName = $"p{paramCounter++}";
            paramMapping[baseParamName] = newParamName;
            newSqlCondition = newSqlCondition.Replace($"@{baseParamName}", $"@{newParamName}");
            _parameters.Add(newParamName, parameters.Get<object>(paramName));
        }

        _whereClauses.Add(newSqlCondition);

        return this;
    }

    /// <summary>
    /// Orders the query results by a specified property.
    /// </summary>
    /// <typeparam name="TKey">The type of the property to order by.</typeparam>
    /// <param name="orderByExpression">An expression specifying the property to order by.</param>
    /// <param name="descending">If true, orders the results in descending order; otherwise, ascending.</param>
    /// <returns>The current <see cref="DapperProjectionQuery{T, TResult}"/> instance for method chaining.</returns>
    public DapperProjectionQuery<T, TResult> OrderBy<TKey>(Expression<Func<T, TKey>> orderByExpression, bool descending = false)
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

    /// <summary>
    /// Paginates the query results.
    /// </summary>
    /// <param name="pageIndex">The zero-based index of the page to retrieve.</param>
    /// <param name="pageSize">The number of records per page.</param>
    /// <returns>The current <see cref="DapperProjectionQuery{T, TResult}"/> instance for method chaining.</returns>
    public DapperProjectionQuery<T, TResult> Paginate(int pageIndex, int pageSize)
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

    /// <summary>
    /// Executes the query and returns the projected results.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation, containing the projected query results.</returns>
    public async Task<IEnumerable<TResult>> ExecuteAsync()
    {
        var sqlBuilder = new StringBuilder();
        var selectColumns = new List<string> { $"t.*" };
        var joins = new List<string>();

        var pkProperty = _propertyMap.FirstOrDefault(p => p.Value.GetCustomAttribute<KeyAttribute>() != null);
        if (pkProperty.Value == null)
        {
            throw new InvalidOperationException("Primary key not found for main entity.");
        }
        string pkColumn = pkProperty.Key;

        // Apply pagination to the main table if needed
        string fromClause = _fullTableName;
        if (_pageIndex.HasValue && _pageSize.HasValue)
        {
            var subQuery = new StringBuilder();
            subQuery.Append($"SELECT * FROM {_fullTableName}");
            if (_whereClauses.Any())
            {
                subQuery.Append($" WHERE {string.Join(" AND ", _whereClauses)}");
            }
            if (_orderByClause.Length > 0)
            {
                subQuery.Append($" {_orderByClause}");
            }
            subQuery.Append($" LIMIT {_pageSize.Value} OFFSET {_pageIndex.Value * _pageSize.Value}");
            fromClause = $"({subQuery})";
            _whereClauses.Clear(); // Clear where clauses as they are applied in the subquery
        }

        // Always include the primary key column for proper entity tracking
        if (_selectedColumns.Count > 0 && !_selectedColumns.Any(c => c.Contains(pkColumn)))
        {
            _selectedColumns.Add($"t.{pkColumn}");
        }

        // Build the SQL query
        if (_selectedColumns.Count > 0)
        {
            sqlBuilder.Append($"SELECT {string.Join(", ", _selectedColumns)}");
        }
        else
        {
            sqlBuilder.Append($"SELECT {string.Join(", ", selectColumns)}");
        }
        
        sqlBuilder.Append($" FROM {fromClause} t");

        if (_whereClauses.Any())
        {
            sqlBuilder.Append($" WHERE {string.Join(" AND ", _whereClauses)}");
        }

        if (_orderByClause.Length > 0)
        {
            sqlBuilder.Append($" {_orderByClause}");
        }

        string sql = sqlBuilder.ToString();

        // Execute the query and get the entities
        var entities = await _connection.QueryAsync<T>(sql, _parameters, transaction: _transaction);

        // Apply the projection using the compiled selector
        return entities.Select(_compiledSelector);
    }
}