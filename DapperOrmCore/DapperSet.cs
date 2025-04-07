using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;
using Dapper;
using System.Text.RegularExpressions;
using System.Linq.Expressions;

namespace DapperOrmCore;

public class DapperSet<T> : IDisposable where T : class
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;
    private readonly string _fullTableName;
    private readonly string _schemaName;
    private readonly string _tableName;
    private readonly Dictionary<string, PropertyInfo> _propertyMap;
    private readonly string _primaryKeyColumnName;
    private readonly Type _primaryKeyType;
    private bool _disposed = false;

    public DapperSet(IDbConnection connection, IDbTransaction transaction = null)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _transaction = transaction;

        // Validate full table name (with schema if present)
        string rawTableName = typeof(T).GetCustomAttribute<TableAttribute>()?.Name ?? typeof(T).Name;
        ParseTableName(rawTableName, out string schema, out string table);
        _schemaName = schema;
        _tableName = ValidateTableName(table);
        _fullTableName = string.IsNullOrEmpty(_schemaName) ? _tableName : $"{_schemaName}.{_tableName}";

        // Property mappings with validation
        _propertyMap = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .ToDictionary(
                p => ValidateColumnName(p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name),
                p => p,
                StringComparer.OrdinalIgnoreCase
            );

        // Primary key using the database column name and type
        var primaryKeyProperty = typeof(T).GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);
        if (primaryKeyProperty == null)
        {
            throw new InvalidOperationException("No primary key found. Please mark a property with [Key].");
        }

        _primaryKeyColumnName = primaryKeyProperty.GetCustomAttribute<ColumnAttribute>()?.Name ?? primaryKeyProperty.Name;
        _primaryKeyType = primaryKeyProperty.PropertyType;

        if (!_propertyMap.ContainsKey(_primaryKeyColumnName))
        {
            throw new InvalidOperationException($"Primary key column '{_primaryKeyColumnName}' not found in entity properties");
        }

        // Dapper column-to-property mapping
        SqlMapper.SetTypeMap(
            typeof(T),
            new CustomPropertyTypeMap(
                typeof(T),
                (type, columnName) =>
                    _propertyMap.TryGetValue(columnName, out var property) ? property : null
            )
        );
    }

    // Parse schema.table notation
    private void ParseTableName(string rawName, out string schema, out string table)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            throw new ArgumentException("Table name cannot be empty or whitespace.");
        }

        var parts = rawName.Split('.');
        if (parts.Length > 2)
        {
            throw new ArgumentException($"Invalid table name format: {rawName}. Use 'schema.table' or 'table' format.");
        }

        if (parts.Length == 2)
        {
            schema = ValidateSchemaName(parts[0]);
            table = parts[1];
        }
        else
        {
            schema = null;
            table = rawName;
        }
    }

    public async Task<T> GetByIdWithRelatedAsync<TKey>(TKey id, string relatedTableFullName, string foreignKey, string splitOn)
    {
        EnsureNotDisposed();
        string sql = $@"
            SELECT t.*, r.*
            FROM {_fullTableName} t
            LEFT JOIN {relatedTableFullName} r ON t.{_primaryKeyColumnName} = r.{foreignKey}
            WHERE t.{_primaryKeyColumnName} = @Id";

        var lookup = new Dictionary<TKey, T>();
        await _connection.QueryAsync<T, dynamic, T>(
            sql,
            (entity, related) =>
            {
                var keyValue = (TKey)_propertyMap[_primaryKeyColumnName].GetValue(entity);
                if (!lookup.TryGetValue(keyValue, out T existing))
                {
                    lookup[keyValue] = entity;
                    existing = entity;
                }
                return existing;
            },
            new { Id = id },
            transaction: _transaction,
            splitOn: splitOn
        );

        return lookup.Values.FirstOrDefault();
    }

    public async Task<IEnumerable<T?>> GetAllAsync()
    {
        EnsureNotDisposed();
        string sql = $"SELECT * FROM {_fullTableName}";
        return await _connection.QueryAsync<T>(sql, transaction: _transaction);
    }

    public async Task<T> GetByIdAsync<TKey>(TKey id)
    {
        EnsureNotDisposed();
        if (id == null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        if (typeof(TKey) != _primaryKeyType)
        {
            throw new ArgumentException($"TKey type '{typeof(TKey).Name}' does not match primary key type '{_primaryKeyType.Name}'");
        }

        string sql = $"SELECT * FROM {_fullTableName} WHERE {_primaryKeyColumnName} = @Id";
        return await _connection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id }, _transaction);
    }

    public async Task<TKey> InsertAsync<TKey>(T entity)
    {
        EnsureNotDisposed();
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        if (typeof(TKey) != _primaryKeyType)
        {
            throw new ArgumentException($"TKey type '{typeof(TKey).Name}' does not match primary key type '{_primaryKeyType.Name}'");
        }

        var columns = _propertyMap
            .Where(p => _primaryKeyType == typeof(string) ? true : !string.Equals(p.Key, _primaryKeyColumnName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (columns.Length == 0)
        {
            throw new InvalidOperationException("No columns found to insert.");
        }

        string columnNames = string.Join(", ", columns.Select(p => p.Key));
        string paramNames = string.Join(", ", columns.Select(p => "@" + p.Value.Name));

        var sql = $"INSERT INTO {_fullTableName} ({columnNames}) VALUES ({paramNames}) RETURNING {_primaryKeyColumnName};";

        try
        {
            var result = await _connection.ExecuteScalarAsync<TKey>(sql, entity, _transaction);
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to insert entity into {_fullTableName}", ex);
        }
    }

    public async Task<bool> UpdateAsync(T entity)
    {
        EnsureNotDisposed();
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        var columns = _propertyMap
            .Where(p => !string.Equals(p.Key, _primaryKeyColumnName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        string setClause = string.Join(", ", columns.Select(p => $"{p.Key} = @{p.Value.Name}"));
        string sql = $"UPDATE {_fullTableName} SET {setClause} WHERE {_primaryKeyColumnName} = @{GetPropertyName(_primaryKeyColumnName)}";

        int rowsAffected = await _connection.ExecuteAsync(sql, entity, _transaction);
        if (rowsAffected == 0)
        {
            throw new InvalidOperationException("No rows were updated. Entity may not exist.");
        }

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync<TKey>(TKey id)
    {
        EnsureNotDisposed();
        if (id == null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        if (typeof(TKey) != _primaryKeyType)
        {
            throw new ArgumentException($"TKey type '{typeof(TKey).Name}' does not match primary key type '{_primaryKeyType.Name}'");
        }

        string sql = $"DELETE FROM {_fullTableName} WHERE {_primaryKeyColumnName} = @Id";
        int rowsAffected = await _connection.ExecuteAsync(sql, new { Id = id }, _transaction);
        if (rowsAffected == 0)
        {
            throw new InvalidOperationException("No rows were deleted. Entity may not exist.");
        }

        return rowsAffected > 0;
    }

    // Updated Where method to return DapperQuery<T> for chaining
    public DapperQuery<T> Where(Expression<Func<T, bool>> predicate)
    {
        EnsureNotDisposed();
        return new DapperQuery<T>(this, _connection, _transaction, _fullTableName, _propertyMap).Where(predicate);
    }

    // New OrderBy method to return DapperQuery<T> for chaining
    public DapperQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> orderByExpression, bool descending = false)
    {
        EnsureNotDisposed();
        return new DapperQuery<T>(this, _connection, _transaction, _fullTableName, _propertyMap)
            .OrderBy(orderByExpression, descending);
    }

    // Original WhereAsync (kept for backward compatibility, if needed)
    [Obsolete("Use the new DapperQuery where syntax instead.")]
    public async Task<IEnumerable<T>> WhereAsync(Expression<Func<T, bool>> predicate)
    {
        EnsureNotDisposed();
        var visitor = new WhereExpressionVisitor<T>(_propertyMap);
        var (sqlCondition, parameters) = visitor.Translate(predicate);

        string sql = $"SELECT * FROM {_fullTableName} WHERE {sqlCondition}";
        return await _connection.QueryAsync<T>(sql, parameters, transaction: _transaction);
    }

    private string ValidateSchemaName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Schema name cannot be empty or whitespace.");
        }

        if (!Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
        {
            throw new ArgumentException($"Invalid schema name: {name}. Only alphanumeric characters and underscores are allowed.");
        }

        return name;
    }

    private string ValidateTableName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Table name cannot be empty or whitespace.");
        }

        if (!Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
        {
            throw new ArgumentException($"Invalid table name: {name}. Only alphanumeric characters and underscores are allowed.");
        }

        return name;
    }

    private string ValidateColumnName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Column name cannot be empty or whitespace.");
        }

        if (!Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
        {
            throw new ArgumentException($"Invalid column name: {name}. Only alphanumeric characters and underscores are allowed.");
        }

        return name;
    }

    private string GetPropertyName(string columnName)
    {
        return _propertyMap[columnName].Name;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Maybe??
            }
            _disposed = true;
        }
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(DapperSet<T>));
        }
    }
}