using Dapper;
using System;
using TimeTracker.Data;
using TimeTracker.Models;

namespace TimeTracker.Services;

public class UsageService
{
    private readonly Database _db;

    public UsageService(Database db)
    {
        _db = db;
    }

    public void AddSession(UsageSession session)
    {
        using var conn = _db.CreateConnection();

        using var transaction = conn.BeginTransaction();

        // 1. вставка сессии
        conn.Execute(@"
        INSERT INTO UsageSessions
        (ApplicationId, StartTime, EndTime, DurationSeconds, CreatedAt)
        VALUES (@ApplicationId, @StartTime, @EndTime, @DurationSeconds, @CreatedAt)",
            session, transaction);

        // 2. обновление агрегата 
        conn.Execute(@"
        INSERT INTO DailyAppStats (ApplicationId, Date, TotalDurationSeconds, SessionsCount, UpdatedAt)
        VALUES (@ApplicationId, Date(@StartTime), @DurationSeconds, 1, @now)
        ON CONFLICT(ApplicationId, Date) DO UPDATE SET
            TotalDurationSeconds = TotalDurationSeconds + @DurationSeconds,
            SessionsCount = SessionsCount + 1,
            UpdatedAt = @now;",
            new
            {
                session.ApplicationId,
                session.StartTime,
                session.DurationSeconds,
                now = DateTime.Now
            },
            transaction);

        transaction.Commit();
    }

    public int GetOrCreateApplication(string processName)
    {
        using var conn = _db.CreateConnection();

        var app = conn.QueryFirstOrDefault<Application>(
            "SELECT * FROM Applications WHERE ProcessName = @name",
            new { name = processName });

        if (app != null)
            return app.Id;

        int newId = conn.ExecuteScalar<int>(@"
            INSERT INTO Applications 
            (ProcessName, DisplayName, CategoryId, IsActive, CreatedAt)
            VALUES (@ProcessName, @DisplayName, 1, 1, @CreatedAt);
            SELECT last_insert_rowid();",
            new
            {
                ProcessName = processName,
                DisplayName = processName,
                CreatedAt = DateTime.Now
            });

        return newId;
    }
}