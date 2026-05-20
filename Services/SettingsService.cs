using Dapper;
using System;
using System.Collections.Generic;
using System.Text;
using TimeTracker.Data;

namespace TimeTracker.Services;

public class SettingsService
{
    private readonly Database _db;

    public SettingsService(Database db)
    {
        _db = db;
    }

    public string? Get(string key)
    {
        using var conn = _db.CreateConnection();

        return conn.QueryFirstOrDefault<string>(
            "SELECT Value FROM Settings WHERE Key = @Key",
            new { Key = key });
    }

    public void Set(string key, string value)
    {
        using var conn = _db.CreateConnection();

        conn.Execute(@"
            INSERT INTO Settings(Key, Value)
            VALUES(@Key, @Value)
            ON CONFLICT(Key)
            DO UPDATE SET Value = excluded.Value",
            new
            {
                Key = key,
                Value = value
            });
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        var value = Get(key);

        return int.TryParse(value, out var result)
            ? result
            : defaultValue;
    }

    public bool GetBool(string key, bool defaultValue = false)
    {
        var value = Get(key);

        return bool.TryParse(value, out var result)
            ? result
            : defaultValue;
    }
}
