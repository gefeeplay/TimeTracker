using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;
using TimeTracker.Data;
using TimeTracker.Models;

namespace TimeTracker.Services;

public class AiCacheService
{
    private readonly Database _db;

    public AiCacheService(Database db)
    {
        _db = db;
    }

    public AiInsightsCache? Get(string scope)
    {
        using var conn = _db.CreateConnection();

        return conn.QueryFirstOrDefault<AiInsightsCache>(
            """
            SELECT *
            FROM AiInsightsCache
            WHERE Scope = @scope
            """,
            new
            {
                scope
            });
    }

    public void Save(string scope, string content)
    {
        using var conn = _db.CreateConnection();

        conn.Execute(
            """
            INSERT INTO AiInsightsCache
                (Scope, Content, UpdatedAt)
            VALUES
                (@scope, @content, @updatedAt)

            ON CONFLICT(Scope) DO UPDATE SET
                Content = excluded.Content,
                UpdatedAt = excluded.UpdatedAt;
            """,
            new
            {
                scope,
                content,
                updatedAt = DateTime.Now
            });
    }
}
