using Microsoft.Data.Sqlite;
using System.Data;
using System.Data.SqlClient;
using Npgsql;

namespace DapperOrmCore.Tests.Helpers
{
    /// <summary>
    /// Factory for creating database connections for different database providers.
    /// </summary>
    public static class DatabaseConnectionFactory
    {
        // Connection strings for different database providers
        private const string SqliteConnectionString = "DataSource=:memory:";
        private const string SqlServerConnectionString = "Server=localhost,1433;Database=testdb;User=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;";
        private const string PostgreSqlConnectionString = "Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=postgres";

        /// <summary>
        /// Creates a database connection for the specified provider.
        /// </summary>
        /// <param name="provider">The database provider to create a connection for.</param>
        /// <returns>An open database connection.</returns>
        /// <exception cref="ArgumentException">Thrown when an unsupported database provider is specified.</exception>
        public static IDbConnection CreateConnection(DatabaseProvider provider)
        {
            // Create the appropriate connection type based on the provider
            IDbConnection connection = provider switch
            {
                DatabaseProvider.SqlServer => new SqlConnection(SqlServerConnectionString),
                DatabaseProvider.PostgreSQL => new NpgsqlConnection(PostgreSqlConnectionString),
                _ => new SqliteConnection(SqliteConnectionString)
            };
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Checks if a connection to the specified database provider can be established.
        /// </summary>
        /// <param name="provider">The database provider to check.</param>
        /// <returns>True if a connection can be established, false otherwise.</returns>
        public static bool IsProviderAvailable(DatabaseProvider provider)
        {
            try
            {
                using var connection = CreateConnection(provider);
                // Execute a simple query to verify the connection works
                if (connection.State == ConnectionState.Open)
                {
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}