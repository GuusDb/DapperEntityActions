namespace DapperOrmCore;

/// <summary>
/// Specifies the database provider to use for generating SQL syntax.
/// </summary>
public enum DatabaseProvider
{
    /// <summary>
    /// SQLite database provider. Uses LIMIT/OFFSET for pagination.
    /// </summary>
    SQLite,
    
    /// <summary>
    /// SQL Server database provider. Uses OFFSET/FETCH for pagination.
    /// </summary>
    SqlServer,
    
    /// <summary>
    /// PostgreSQL database provider. Uses LIMIT/OFFSET for pagination.
    /// </summary>
    PostgreSQL
}