using DapperOrmCore.Tests.Helpers;
using DapperOrmCore.Tests.Models;
using System.Data;
using Xunit;
using Dapper;

namespace DapperOrmCore.Tests.IntegrationTests
{
    /// <summary>
    /// Integration tests for insert operations with real database connections.
    /// These tests require Docker containers for SQL Server and PostgreSQL to be running.
    /// </summary>
    public class RealDatabaseInsertTests : IDisposable
    {
        private readonly List<IDbConnection> _connections = new List<IDbConnection>();

        /// <summary>
        /// Tests insert operations with a real SQL Server database using OUTPUT INSERTED syntax.
        /// </summary>
        [Fact]
        public async Task InsertAsync_WithRealSqlServer_ShouldUseOutputInsertedSyntax()
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

            // Create context and test insert
            using var dbContext = new ApplicationDbContext(connection, DatabaseProvider.SqlServer);

            // Test string primary key insert
            var newPlant = new Plant
            {
                PlantCd = "SQLSRV_PLANT",
                Description = "SQL Server Plant",
                IsAcive = true
            };

            var insertedId = await dbContext.Plants.InsertAsync<string>(newPlant);
            var retrieved = await dbContext.Plants.GetByIdAsync<string>("SQLSRV_PLANT");

            // Assert
            Assert.Equal("SQLSRV_PLANT", insertedId);
            Assert.NotNull(retrieved);
            Assert.Equal("SQL Server Plant", retrieved.Description);
            Assert.True(retrieved.IsAcive);

            // Test auto-increment primary key insert
            var newMeasurement = new CoolMeasurement
            {
                TestCd = "TEST1",
                PlantCd = "SQLSRV_PLANT",
                Value = 123.45,
                MeasurementDate = DateTime.UtcNow
            };

            var measurementId = await dbContext.Measurements.InsertAsync<int>(newMeasurement);
            var retrievedMeasurement = await dbContext.Measurements.GetByIdAsync<int>(measurementId);

            Assert.True(measurementId > 0);
            Assert.NotNull(retrievedMeasurement);
            Assert.Equal(123.45, retrievedMeasurement.Value);
        }

        /// <summary>
        /// Tests insert operations with a real PostgreSQL database using RETURNING syntax.
        /// </summary>
        [Fact]
        public async Task InsertAsync_WithRealPostgreSQL_ShouldUseReturningSyntax()
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

            // Create context and test insert
            using var dbContext = new ApplicationDbContext(connection, DatabaseProvider.PostgreSQL);

            // Test string primary key insert
            var newPlant = new Plant
            {
                PlantCd = "POSTGRES_PLANT",
                Description = "PostgreSQL Plant",
                IsAcive = true
            };

            var insertedId = await dbContext.Plants.InsertAsync<string>(newPlant);
            var retrieved = await dbContext.Plants.GetByIdAsync<string>("POSTGRES_PLANT");

            // Assert
            Assert.Equal("POSTGRES_PLANT", insertedId);
            Assert.NotNull(retrieved);
            Assert.Equal("PostgreSQL Plant", retrieved.Description);
            Assert.True(retrieved.IsAcive);

            // Test auto-increment primary key insert
            var newMeasurement = new CoolMeasurement
            {
                TestCd = "TEST1",
                PlantCd = "POSTGRES_PLANT",
                Value = 456.78,
                MeasurementDate = DateTime.UtcNow
            };

            var measurementId = await dbContext.Measurements.InsertAsync<int>(newMeasurement);
            var retrievedMeasurement = await dbContext.Measurements.GetByIdAsync<int>(measurementId);

            Assert.True(measurementId > 0);
            Assert.NotNull(retrievedMeasurement);
            Assert.Equal(456.78, retrievedMeasurement.Value);
        }

        /// <summary>
        /// Tests that SQL Server correctly handles multiple inserts and returns proper IDs.
        /// </summary>
        [Fact]
        public async Task InsertAsync_SqlServer_MultipleInserts_ShouldReturnCorrectIds()
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

            // Create context
            using var dbContext = new ApplicationDbContext(connection, DatabaseProvider.SqlServer);

            // Insert multiple measurements and verify IDs are sequential
            var measurements = new List<CoolMeasurement>();
            var insertedIds = new List<int>();

            for (int i = 1; i <= 5; i++)
            {
                var measurement = new CoolMeasurement
                {
                    TestCd = "TEST1",
                    PlantCd = "PLANT1",
                    Value = i * 10.0,
                    MeasurementDate = DateTime.UtcNow.AddMinutes(i)
                };

                var id = await dbContext.Measurements.InsertAsync<int>(measurement);
                insertedIds.Add(id);
                measurements.Add(measurement);
            }

            // Verify all IDs are unique and greater than 0
            Assert.Equal(5, insertedIds.Distinct().Count());
            Assert.All(insertedIds, id => Assert.True(id > 0));

            // Verify we can retrieve all inserted measurements
            for (int i = 0; i < insertedIds.Count; i++)
            {
                var retrieved = await dbContext.Measurements.GetByIdAsync<int>(insertedIds[i]);
                Assert.NotNull(retrieved);
                Assert.Equal((i + 1) * 10.0, retrieved.Value);
            }
        }

        /// <summary>
        /// Tests that PostgreSQL correctly handles multiple inserts and returns proper IDs.
        /// </summary>
        [Fact]
        public async Task InsertAsync_PostgreSQL_MultipleInserts_ShouldReturnCorrectIds()
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

            // Create context
            using var dbContext = new ApplicationDbContext(connection, DatabaseProvider.PostgreSQL);

            // Insert multiple measurements and verify IDs are sequential
            var measurements = new List<CoolMeasurement>();
            var insertedIds = new List<int>();

            for (int i = 1; i <= 5; i++)
            {
                var measurement = new CoolMeasurement
                {
                    TestCd = "TEST1",
                    PlantCd = "PLANT1",
                    Value = i * 20.0,
                    MeasurementDate = DateTime.UtcNow.AddMinutes(i)
                };

                var id = await dbContext.Measurements.InsertAsync<int>(measurement);
                insertedIds.Add(id);
                measurements.Add(measurement);
            }

            // Verify all IDs are unique and greater than 0
            Assert.Equal(5, insertedIds.Distinct().Count());
            Assert.All(insertedIds, id => Assert.True(id > 0));

            // Verify we can retrieve all inserted measurements
            for (int i = 0; i < insertedIds.Count; i++)
            {
                var retrieved = await dbContext.Measurements.GetByIdAsync<int>(insertedIds[i]);
                Assert.NotNull(retrieved);
                Assert.Equal((i + 1) * 20.0, retrieved.Value);
            }
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

                string cleanupSql = provider switch
                {
                    DatabaseProvider.SqlServer => @"
                        IF OBJECT_ID('measurement', 'U') IS NOT NULL
                        BEGIN
                            DELETE FROM measurement;
                            DROP TABLE measurement;
                        END
                        IF OBJECT_ID('plant', 'U') IS NOT NULL
                        BEGIN
                            DELETE FROM plant;
                            DROP TABLE plant;
                        END
                        IF OBJECT_ID('test', 'U') IS NOT NULL
                        BEGIN
                            DELETE FROM test;
                            DROP TABLE test;
                        END",
                    DatabaseProvider.PostgreSQL => @"
                        DROP TABLE IF EXISTS measurement CASCADE;
                        DROP TABLE IF EXISTS plant CASCADE;
                        DROP TABLE IF EXISTS test CASCADE;",
                    _ => throw new ArgumentException($"Unsupported database provider: {provider}")
                };

                connection.Execute(cleanupSql);
            }
            catch
            {
                // Ignore cleanup errors - tables might not exist
            }

            // Create tables based on provider
            string createTablesSql = provider switch
            {
                DatabaseProvider.SqlServer => @"
                    CREATE TABLE plant (
                        plant_cd NVARCHAR(50) PRIMARY KEY,
                        description NVARCHAR(255),
                        is_active BIT
                    );
                    
                    CREATE TABLE test (
                        test_cd NVARCHAR(50) PRIMARY KEY,
                        description NVARCHAR(255),
                        is_active BIT,
                        test_type_cd NVARCHAR(50),
                        test_mode_cd NVARCHAR(50),
                        precision INT,
                        created_date DATETIME2,
                        last_edit_date DATETIME2
                    );
                    
                    CREATE TABLE measurement (
                        id INT IDENTITY(1,1) PRIMARY KEY,
                        test_cd NVARCHAR(50),
                        plant_cd NVARCHAR(50),
                        avg_value FLOAT,
                        measurement_date DATETIME2
                    );",
                DatabaseProvider.PostgreSQL => @"
                    CREATE TABLE plant (
                        plant_cd TEXT PRIMARY KEY,
                        description TEXT,
                        is_active BOOLEAN
                    );
                    
                    CREATE TABLE test (
                        test_cd TEXT PRIMARY KEY,
                        description TEXT,
                        is_active BOOLEAN,
                        test_type_cd TEXT,
                        test_mode_cd TEXT,
                        precision INTEGER,
                        created_date TIMESTAMP,
                        last_edit_date TIMESTAMP
                    );
                    
                    CREATE TABLE measurement (
                        id SERIAL PRIMARY KEY,
                        test_cd TEXT,
                        plant_cd TEXT,
                        avg_value DOUBLE PRECISION,
                        measurement_date TIMESTAMP
                    );",
                _ => throw new ArgumentException($"Unsupported database provider: {provider}")
            };

            connection.Execute(createTablesSql);

            // Insert basic test data
            string insertPlantSql = provider switch
            {
                DatabaseProvider.SqlServer => @"
                    INSERT INTO plant (plant_cd, description, is_active)
                    VALUES ('PLANT1', 'Plant 1', 1)",
                DatabaseProvider.PostgreSQL => @"
                    INSERT INTO plant (plant_cd, description, is_active)
                    VALUES ('PLANT1', 'Plant 1', true)",
                _ => throw new ArgumentException($"Unsupported database provider: {provider}")
            };

            string insertTestSql = provider switch
            {
                DatabaseProvider.SqlServer => @"
                    INSERT INTO test (test_cd, description, is_active, test_type_cd, test_mode_cd, precision, created_date, last_edit_date)
                    VALUES ('TEST1', 'Test 1', 1, 'Dimensional', 'InProcess', 80, GETDATE(), GETDATE())",
                DatabaseProvider.PostgreSQL => @"
                    INSERT INTO test (test_cd, description, is_active, test_type_cd, test_mode_cd, precision, created_date, last_edit_date)
                    VALUES ('TEST1', 'Test 1', true, 'Dimensional', 'InProcess', 80, NOW(), NOW())",
                _ => throw new ArgumentException($"Unsupported database provider: {provider}")
            };

            connection.Execute(insertPlantSql);
            connection.Execute(insertTestSql);
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