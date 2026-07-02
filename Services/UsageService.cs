using Dapper;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using TimeTracker.Data;
using TimeTracker.Models;
using TimeTracker.Services;
using Windows.Graphics.Imaging;

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

    public int GetOrCreateApplication(string processName, string displayName, string? exePath)
    {
        using var conn = _db.CreateConnection();

        var app = conn.QueryFirstOrDefault<Application>(
            "SELECT * FROM Applications WHERE ProcessName = @name",
            new { name = processName });

        // ПРИЛОЖЕНИЕ УЖЕ ЕСТЬ, НО ИМЯ КАК ПРОЦЕСС (старая версия) → обновляем имя
        if (app != null)
        {
            if (string.IsNullOrWhiteSpace(app.DisplayName)
                || app.DisplayName == app.ProcessName)
            {
                conn.Execute(
                    @"UPDATE Applications
              SET DisplayName = @DisplayName
              WHERE Id = @Id",
                    new
                    {
                        Id = app.Id,
                        DisplayName = displayName
                    });
            }

            return app.Id;
        }

        if (app != null)
            return app.Id;

        // НОВОЕ ПРИЛОЖЕНИЕ → получаем иконку
        string? iconPath = null;

        if (!string.IsNullOrEmpty(exePath))
        {
            iconPath = IconHelper.SaveIconToFile(exePath, processName);
        }

        int newId = conn.ExecuteScalar<int>(@"
            INSERT INTO Applications 
            (ProcessName, DisplayName, CategoryId, IsActive, CreatedAt, IconPath)
            VALUES (@ProcessName, @DisplayName, 1, 1, @CreatedAt, @IconPath);
            SELECT last_insert_rowid();",
            new
            {
                ProcessName = processName,
                DisplayName = displayName,
                CreatedAt = DateTime.Now,
                IconPath = iconPath
            });

        return newId;
    }
}