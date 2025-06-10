using Dapper;
using DapperOrmCore;
using DapperOrmCore.Tests.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Threading.Tasks;

namespace DapperOrmCore.Tests;

public abstract class TestSetup : IDisposable
{
    protected readonly SqliteConnection Connection;
    protected readonly ApplicationDbContext DbContext;

    protected TestSetup()
    {
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();
        DbContext = new ApplicationDbContext(Connection);
        CreateTables();
        SeedData();
    }

    private void CreateTables()
    {
        Connection.Execute(@"
            CREATE TABLE plant (
                plant_cd TEXT PRIMARY KEY,
                description TEXT,
                is_active INTEGER
            )");

        Connection.Execute(@"
            CREATE TABLE test (
                test_cd TEXT PRIMARY KEY,
                description TEXT,
                is_active INTEGER,
                test_type_cd TEXT,
                test_mode_cd TEXT,
                precision INTEGER,
                created_date TEXT,
                last_edit_date TEXT
            )");

        Connection.Execute(@"
            CREATE TABLE measurement (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                test_cd TEXT,
                plant_cd TEXT,
                avg_value REAL,
                measurement_date TEXT,
                FOREIGN KEY (test_cd) REFERENCES test(test_cd),
                FOREIGN KEY (plant_cd) REFERENCES plant(plant_cd)
            )");

        Connection.Execute(@"
            CREATE TABLE parent (
                parent_id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )");

        Connection.Execute(@"
            CREATE TABLE child (
                child_id INTEGER PRIMARY KEY AUTOINCREMENT,
                parent_id INTEGER,
                name TEXT NOT NULL,
                is_active INTEGER,
                FOREIGN KEY (parent_id) REFERENCES parent(parent_id)
            )");
            
        Connection.Execute(@"
            CREATE TABLE auditable_entity (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                created_on TEXT
            )");
            
        Connection.Execute(@"
            CREATE TABLE entity_with_created_date (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                created_date TEXT
            )");
            
        Connection.Execute(@"
            CREATE TABLE custom_property_entity (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                custom_date_field TEXT
            )");
    }

    private void SeedData()
    {
        Connection.Execute(@"
            INSERT INTO plant (plant_cd, description, is_active)
            VALUES 
                ('PLANT1', 'Plant 1', 1),
                ('PLANT2', 'Plant 2', 1),
                ('PLANT3', 'Plant 3', 0),
                ('PLANT4', 'Plant 4', 1)");

        Connection.Execute(@"
            INSERT INTO test (test_cd, description, is_active, test_type_cd, test_mode_cd, precision, created_date, last_edit_date)
            VALUES 
                ('TEST1', 'Test 1', 1, 'Dimensional', 'InProcess', 80, '2025-04-20T00:00:00Z', '2025-04-20T00:00:00Z'),
                ('TEST2', 'Test 2', 1, 'Dimensional', 'Offline', 85, '2025-04-21T00:00:00Z', '2025-04-21T00:00:00Z'),
                ('TEST3', 'Test 3', 0, 'Functional', 'InProcess', 90, '2025-04-22T00:00:00Z', '2025-04-22T00:00:00Z'),
                ('TEST4', 'Test 4', 1, 'Functional', 'Offline', 95, '2025-04-23T00:00:00Z', '2025-04-23T00:00:00Z')");

        Connection.Execute(@"
            INSERT INTO measurement (test_cd, plant_cd, avg_value, measurement_date)
            VALUES 
                ('TEST1', 'PLANT1', 150.5, '2025-04-22T10:00:00Z'),
                ('TEST1', 'PLANT2', 200.0, '2025-04-22T11:00:00Z'),
                ('TEST2', 'PLANT1', 50.0, '2025-04-22T12:00:00Z'),
                ('TEST2', 'PLANT2', 300.0, '2025-04-22T13:00:00Z'),
                ('TEST3', 'PLANT3', 100.0, '2025-04-22T14:00:00Z'),
                ('TEST4', 'PLANT4', 250.0, '2025-04-22T15:00:00Z')");

        Connection.Execute(@"
            INSERT INTO parent (parent_id, name)
            VALUES 
                (1, 'Parent1'),
                (2, 'Parent2'),
                (3, 'Parent3')");

        Connection.Execute(@"
            INSERT INTO child (parent_id, name, is_active)
            VALUES 
                (1, 'Child1', 1),
                (1, 'Child2', 0),
                (2, 'Child3', 1),
                (2, 'Child4', 1),
                (3, 'Child5', 0)");
    }

    public void Dispose()
    {
        DbContext?.Dispose();
        Connection?.Dispose();
    }
}