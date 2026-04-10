using Dapper;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;

public class Database
{
    private readonly string _connectionString;

    public Database(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
    }

    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        cmd.ExecuteNonQuery();

        return connection;
    }

    // 🔹 Универсальные методы (опционально)

    public int Execute(string sql, object? param = null)
    {
        System.Diagnostics.Debug.WriteLine("SQL: " + sql);

        using var conn = CreateConnection();
        return conn.Execute(sql, param);
    }

    public T QuerySingle<T>(string sql, object? param = null)
    {
        using var conn = CreateConnection();
        return conn.QuerySingle<T>(sql, param);
    }

    public T? QuerySingleOrDefault<T>(string sql, object? param = null)
    {
        using var conn = CreateConnection();
        return conn.QuerySingleOrDefault<T>(sql, param);
    }

    public IEnumerable<T> Query<T>(string sql, object? param = null)
    {
        using var conn = CreateConnection();
        return conn.Query<T>(sql, param);
    }
}