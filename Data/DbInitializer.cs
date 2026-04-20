using Microsoft.Data.Sqlite;
using System;

public static class DbInitializer
{
    public static void Initialize(string dbPath)
    {
        try
        {
            var connectionString = $"Data Source={dbPath}";

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            System.Diagnostics.Debug.WriteLine("DB OPENED");

            EnableForeignKeys(connection);
            CreateTables(connection);
            CreateIndexes(connection);
            SeedData(connection);

            System.Diagnostics.Debug.WriteLine("DB INITIALIZED");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("DB INIT ERROR: " + ex.Message);
            throw;
        }
    }

    private static void EnableForeignKeys(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        cmd.ExecuteNonQuery();
    }

    private static void CreateTables(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();

        cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS Categories (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL UNIQUE
        );

        CREATE TABLE IF NOT EXISTS Applications (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            ProcessName TEXT NOT NULL,
            DisplayName TEXT,
            CategoryId INTEGER NOT NULL,
            IsActive INTEGER NOT NULL,
            CreatedAt DATETIME NOT NULL,
            IconPath TEXT,
            FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
        );

        CREATE TABLE IF NOT EXISTS UsageSessions (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            ApplicationId INTEGER NOT NULL,
            StartTime DATETIME NOT NULL,
            EndTime DATETIME NOT NULL,
            DurationSeconds INTEGER NOT NULL,
            CreatedAt DATETIME NOT NULL,
            FOREIGN KEY (ApplicationId) REFERENCES Applications(Id)
        );

        CREATE TABLE IF NOT EXISTS DailyAppStats (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            ApplicationId INTEGER NOT NULL,
            Date DATE NOT NULL,
            TotalDurationSeconds INTEGER NOT NULL,
            SessionsCount INTEGER NOT NULL,
            UpdatedAt DATETIME NOT NULL,
            UNIQUE(ApplicationId, Date),
            FOREIGN KEY (ApplicationId) REFERENCES Applications(Id)
        );
        ";

        cmd.ExecuteNonQuery();
    }

    private static void CreateIndexes(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();

        cmd.CommandText = @"
        CREATE INDEX IF NOT EXISTS idx_app_process ON Applications(ProcessName);
        CREATE INDEX IF NOT EXISTS idx_app_category ON Applications(CategoryId);

        CREATE INDEX IF NOT EXISTS idx_sessions_app ON UsageSessions(ApplicationId);
        CREATE INDEX IF NOT EXISTS idx_sessions_start ON UsageSessions(StartTime);
        CREATE INDEX IF NOT EXISTS idx_sessions_end ON UsageSessions(EndTime);

        CREATE INDEX IF NOT EXISTS idx_stats_date ON DailyAppStats(Date);
        CREATE INDEX IF NOT EXISTS idx_stats_app ON DailyAppStats(ApplicationId);
        ";

        cmd.ExecuteNonQuery();
    }

    private static void SeedData(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();

        cmd.CommandText = @"
        INSERT OR IGNORE INTO Categories (Id, Name) VALUES (1, 'Без категории');
        INSERT OR IGNORE INTO Categories (Name) VALUES ('Работа');
        INSERT OR IGNORE INTO Categories (Name) VALUES ('Развлечения');
        INSERT OR IGNORE INTO Categories (Name) VALUES ('Обучение');
        ";

        cmd.ExecuteNonQuery();
    }
}