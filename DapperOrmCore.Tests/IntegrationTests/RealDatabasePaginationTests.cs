using DapperOrmCore.Tests.Helpers;
using DapperOrmCore.Tests.Models;
using System.Data;
using Xunit;
using Dapper;

namespace DapperOrmCore.Tests.IntegrationTests
{
    /// <summary>
    /// Integration tests for pagination with real database connections.
    /// These tests require Docker containers for SQL Server and PostgreSQL to be running.
    /// </summary>
    public class RealDatabasePaginationTests : IDisposable
    {
        private readonly List<IDbConnection> _connections = new List<IDbConnection>();
        
        /// <summary>
        /// Tests pagination with a real SQL Server database.
        /// </summary>
        [Fact]
        public async Task Paginate_WithRealSqlServer_ShouldWork()
        {
            // Skip if SQL Server is not available
            if (!DatabaseConnectionFactory.IsProviderAvailable(DatabaseProvider.SqlServer))
            {
                return;
            }
            
            // Create connection and context
            var connection = DatabaseConnectionFactory.CreateConnection(DatabaseProvider.SqlServer);
            _connections.Add(connection);
            
            // Setup database
            SetupDatabase(connection, DatabaseProvider.SqlServer);
            
            // Create context and test pagination
            using var dbContext = new ApplicationDbContext(connection, DatabaseProvider.SqlServer);
            
            // Test first page
            var page1 = await dbContext.Plants
                .OrderBy(p => p.PlantCd)
                .Paginate(0, 5)
                .ExecuteAsync();
                
            var page1Plants = page1.ToList();
            Assert.Equal(5, page1Plants.Count);
            Assert.Equal("PLANT01", page1Plants[0].PlantCd);
            Assert.Equal("PLANT05", page1Plants[4].PlantCd);
            
            // Test second page
            var page2 = await dbContext.Plants
                .OrderBy(p => p.PlantCd)
                .Paginate(1, 5)
                .ExecuteAsync();
                
            var page2Plants = page2.ToList();
            Assert.Equal(5, page2Plants.Count);
            Assert.Equal("PLANT06", page2Plants[0].PlantCd);
            Assert.Equal("PLANT10", page2Plants[4].PlantCd);
        }
        
        /// <summary>
        /// Tests pagination with a real PostgreSQL database.
        /// </summary>
        [Fact]
        public async Task Paginate_WithRealPostgreSQL_ShouldWork()
        {
            // Skip if PostgreSQL is not available
            if (!DatabaseConnectionFactory.IsProviderAvailable(DatabaseProvider.PostgreSQL))
            {
                return;
            }
            
            // Create connection and context
            var connection = DatabaseConnectionFactory.CreateConnection(DatabaseProvider.PostgreSQL);
            _connections.Add(connection);
            
            // Setup database
            SetupDatabase(connection, DatabaseProvider.PostgreSQL);
            
            // Create context and test pagination
            using var dbContext = new ApplicationDbContext(connection, DatabaseProvider.PostgreSQL);
            
            // Test first page
            var page1 = await dbContext.Plants
                .OrderBy(p => p.PlantCd)
                .Paginate(0, 5)
                .ExecuteAsync();
                
            var page1Plants = page1.ToList();
            Assert.Equal(5, page1Plants.Count);
            Assert.Equal("PLANT01", page1Plants[0].PlantCd);
            Assert.Equal("PLANT05", page1Plants[4].PlantCd);
            
            // Test second page
            var page2 = await dbContext.Plants
                .OrderBy(p => p.PlantCd)
                .Paginate(1, 5)
                .ExecuteAsync();
                
            var page2Plants = page2.ToList();
            Assert.Equal(5, page2Plants.Count);
            Assert.Equal("PLANT06", page2Plants[0].PlantCd);
            Assert.Equal("PLANT10", page2Plants[4].PlantCd);
        }
        
        /// <summary>
        /// Sets up the database for testing.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="provider">The database provider.</param>
        private void SetupDatabase(IDbConnection connection, DatabaseProvider provider)
        {
            try
            {
                // More aggressive cleanup for PostgreSQL
                if (provider == DatabaseProvider.PostgreSQL)
                {
                    // Drop the table and recreate the database schema if needed
                    connection.Execute("DROP TABLE IF EXISTS plant CASCADE;");
                    // Also try to clean up any potential schema issues
                    connection.Execute("DROP SCHEMA IF EXISTS test_schema CASCADE;");
                    connection.Execute("CREATE SCHEMA IF NOT EXISTS test_schema;");
                }
                else if (provider == DatabaseProvider.SqlServer)
                {
                    connection.Execute(@"
                        IF OBJECT_ID('plant', 'U') IS NOT NULL
                        BEGIN
                            DELETE FROM plant;
                            DROP TABLE plant;
                        END");
                }
            }
            catch
            {
                // Ignore cleanup errors - tables might not exist
            }
            
            // Create tables based on provider
            string createTableSql = provider switch
            {
                DatabaseProvider.SqlServer => @"
                    CREATE TABLE plant (
                        plant_cd NVARCHAR(50) PRIMARY KEY,
                        description NVARCHAR(255),
                        is_active BIT
                    )",
                DatabaseProvider.PostgreSQL => @"
                    CREATE TABLE plant (
                        plant_cd TEXT PRIMARY KEY,
                        description TEXT,
                        is_active BOOLEAN
                    )",
                _ => throw new ArgumentException($"Unsupported database provider: {provider}")
            };
            
            connection.Execute(createTableSql);
            
            // Insert test data
            for (int i = 1; i <= 20; i++)
            {
                string insertSql = provider switch
                {
                    DatabaseProvider.SqlServer => @"
                        INSERT INTO plant (plant_cd, description, is_active)
                        VALUES (@PlantCd, @Description, @IsActive)",
                    DatabaseProvider.PostgreSQL => @"
                        INSERT INTO plant (plant_cd, description, is_active)
                        VALUES (@PlantCd, @Description, @IsActive)",
                    _ => throw new ArgumentException($"Unsupported database provider: {provider}")
                };
                
                connection.Execute(insertSql, new {
                    PlantCd = $"PLANT{i:D2}",
                    Description = $"Plant {i}",
                    IsActive = i % 2 == 0
                });
            }
        }
        
        public void Dispose()
        {
            foreach (var connection in _connections)
            {
                connection?.Dispose();
            }
        }
    }
}