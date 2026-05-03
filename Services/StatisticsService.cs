using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using TimeTracker.Data;
using TimeTracker.Models;

namespace TimeTracker.Services;

public class StatisticsService
{
    private readonly Database _db;

    public StatisticsService(Database db)
    {
        _db = db;
    }

    // Статистика за день по приложениям
    public IEnumerable<DailyAppStat> GetStatsByDate(DateTime date)
    {
        using var conn = _db.CreateConnection();

        return conn.Query<DailyAppStat>(@"
        SELECT * FROM DailyAppStats
        WHERE Date = @date
        ORDER BY TotalDurationSeconds DESC",
            new { date = date.ToString("yyyy-MM-dd") });
    }

    // Сырые данные (если нужно)
    public IEnumerable<UsageSession> GetSessions(DateTime from, DateTime to)
    {
        using var conn = _db.CreateConnection();

        return conn.Query<UsageSession>(@"
            SELECT * FROM UsageSessions
            WHERE StartTime >= @from AND EndTime <= @to",
            new { from, to });
    }

    // Топ приложений за период
    public IEnumerable<(string AppName, int TotalSeconds)> GetTopApps(DateTime from, DateTime to)
    {
        using var conn = _db.CreateConnection();

        var result = conn.Query<AppUsageDto>(@"
            SELECT a.DisplayName as AppName,
                   SUM(u.DurationSeconds) as TotalSeconds
            FROM UsageSessions u
            JOIN Applications a ON a.Id = u.ApplicationId
            WHERE u.StartTime >= @from AND u.EndTime <= @to
            GROUP BY a.DisplayName
            ORDER BY TotalSeconds DESC",
            new { from, to });

        return result.Select(x => (x.AppName, x.TotalSeconds));
    }

    // Пересчёт агрегированной статистики (очень важно)
    public void RecalculateDailyStats(DateTime date)
    {
        using var conn = _db.CreateConnection();

        conn.Execute(@"
        INSERT INTO DailyAppStats (ApplicationId, Date, TotalDurationSeconds, SessionsCount, UpdatedAt)
        SELECT 
            ApplicationId,
            DATE(StartTime) as Date,
            SUM(DurationSeconds),
            COUNT(*),
            @now
        FROM UsageSessions
        WHERE DATE(StartTime) = @date
        GROUP BY ApplicationId
        ON CONFLICT(ApplicationId, Date) DO UPDATE SET
            TotalDurationSeconds = excluded.TotalDurationSeconds,
            SessionsCount = excluded.SessionsCount,
            UpdatedAt = excluded.UpdatedAt;",
            new
            {
                date = date.ToString("yyyy-MM-dd"),
                now = DateTime.Now.ToString("yyyy-MM-dd")
            });
    }

    // Получить общее время за день (в секундах)
    public int GetTotalTimeForDate(DateTime date)
    {
        using var conn = _db.CreateConnection();

        //System.Diagnostics.Debug.WriteLine("date: " + date + "\nstart: " + date.ToString("yyyy-MM-dd"));

        return conn.ExecuteScalar<int>(@"
        SELECT COALESCE(SUM(TotalDurationSeconds), 0)
        FROM DailyAppStats
        WHERE Date = @date",
        new { date = date.ToString("yyyy-MM-dd") });
    }

    // Получить данные для графика активности за неделю (по дням)
    public IEnumerable<(DateTime Date, int TotalSeconds)> GetWeeklyActivity() 
    { 
        using var conn = _db.CreateConnection(); 
        var result = conn.Query<DailyActivityDto>(@"
        SELECT Date, SUM(TotalDurationSeconds) as TotalSeconds
        FROM DailyAppStats 
        WHERE Date >= @startDate AND Date <= @endDate 
        GROUP BY Date 
        ORDER BY Date", 
        new 
        { 
            startDate = DateTime.Today.AddDays(-6).ToString("yyyy-MM-dd"), 
            endDate = DateTime.Today.ToString("yyyy-MM-dd")
        });
        //return result.Select(x => ((DateTime)x.Date, (int)x.TotalSeconds)); }
         return result.Select(x => (DateTime.Parse(x.Date), x.TotalSeconds));
    }

    // Получить данные для графика приложения за неделю (по дням)
    public IEnumerable<(DateTime Date, int TotalSeconds)> GetWeeklyActivityByApp(string appName)
    {
        using var conn = _db.CreateConnection();

        var result = conn.Query<DailyActivityDto>(@"
        SELECT d.Date, SUM(d.TotalDurationSeconds) as TotalSeconds
        FROM DailyAppStats d
        JOIN Applications a ON d.ApplicationId = a.Id
        WHERE a.DisplayName = @appName
          AND d.Date >= @startDate AND d.Date <= @endDate
        GROUP BY d.Date
        ORDER BY d.Date",
            new
            {
                appName,
                startDate = DateTime.Today.AddDays(-6).ToString("yyyy-MM-dd"),
                endDate = DateTime.Today.ToString("yyyy-MM-dd")
            });

        return result.Select(x => (DateTime.Parse(x.Date), x.TotalSeconds));
    }

    // Получить все категории
    public IEnumerable<Category> GetCategories()
    {
        using var conn = _db.CreateConnection();
        return conn.Query<Category>("SELECT * FROM Categories ORDER BY Name");
    }

    // Получить приложения с категориями за период
    public IEnumerable<(string AppName, string CategoryName, int TotalSeconds, string? IconPath)> GetAppsWithCategories(DateTime from, DateTime to)
    {
        using var conn = _db.CreateConnection();

        var result = conn.Query<AppWithCategoryDto>(@"
            SELECT 
                a.DisplayName as AppName,
                c.Name as CategoryName,
                SUM(u.DurationSeconds) as TotalSeconds,
                a.IconPath
            FROM UsageSessions u
            JOIN Applications a ON a.Id = u.ApplicationId
            JOIN Categories c ON c.Id = a.CategoryId
            WHERE u.StartTime >= @from AND u.EndTime <= @to
            GROUP BY a.Id, a.DisplayName, c.Name
            ORDER BY TotalSeconds DESC",
            new { from, to });

        return result.Select(x => (x.AppName, x.CategoryName, x.TotalSeconds, x.IconPath));
    }

    // Получить статистику по категориям за период
    public IEnumerable<(string CategoryName, int TotalSeconds, int AppCount)> GetCategoryStats(DateTime from, DateTime to)
    {
        using var conn = _db.CreateConnection();

        var result = conn.Query<CategoryStatDto>(@"
            SELECT 
                c.Name as CategoryName,
                COALESCE(SUM(u.DurationSeconds), 0) as TotalSeconds,
                COUNT(DISTINCT a.Id) as AppCount
            FROM Categories c
            LEFT JOIN Applications a ON a.CategoryId = c.Id
            LEFT JOIN UsageSessions u ON u.ApplicationId = a.Id 
                AND u.StartTime >= @from AND u.EndTime <= @to
            GROUP BY c.Id, c.Name
            HAVING TotalSeconds > 0
            ORDER BY TotalSeconds DESC",
            new { from, to });

        return result.Select(x => (x.CategoryName, x.TotalSeconds, x.AppCount));
    }

    // Получить самое частое приложение за период
    public (string AppName, string CategoryName, int TotalSeconds, string? IconPath)? GetMostFrequentApp(DateTime from, DateTime to)
    {
        using var conn = _db.CreateConnection();

        var result = conn.QueryFirstOrDefault<AppWithCategoryDto>(@"
            SELECT 
                a.DisplayName as AppName,
                c.Name as CategoryName,
                SUM(u.DurationSeconds) as TotalSeconds,
                a.IconPath as IconPath
            FROM UsageSessions u
            JOIN Applications a ON a.Id = u.ApplicationId
            JOIN Categories c ON c.Id = a.CategoryId
            WHERE u.StartTime >= @from AND u.EndTime <= @to
            GROUP BY a.Id, a.DisplayName, c.Name
            ORDER BY TotalSeconds DESC
            LIMIT 1",
            new { from, to });

        if (result == null)
            return null;

        return (result.AppName, result.CategoryName, result.TotalSeconds, result.IconPath);
    }

    // Получить количество переключений окон за период
    public int GetWindowSwitchesCount(DateTime from, DateTime to)
    {
        using var conn = _db.CreateConnection();

        return conn.ExecuteScalar<int>(@"
            SELECT COUNT(*) FROM UsageSessions
            WHERE StartTime >= @from AND EndTime <= @to",
            new { from, to });
    }

    //Все приложения
    public IEnumerable<AppWithCategory> GetAllApplications()
    {
        using var conn = _db.CreateConnection();

        var query = @"
        SELECT 
            a.Id,
            a.DisplayName,
            a.ProcessName,
            a.CategoryId,
            a.IconPath,
            c.Name as CategoryName
        FROM Applications a
        JOIN Categories c ON a.CategoryId = c.Id
        WHERE a.IsActive = 1
        ORDER BY a.DisplayName
    ";

        using var command = conn.CreateCommand();
        command.CommandText = query;

        using var reader = command.ExecuteReader();

        var result = new List<AppWithCategory>();

        while (reader.Read())
        {
            var processName = reader.IsDBNull(2) ? null : reader.GetString(2);

            result.Add(new AppWithCategory
            {
                Id = reader.GetInt32(0),
                AppName = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1),
                CategoryId = reader.GetInt32(3),
                IconPath = reader.GetString(4),
                CategoryName = reader.GetString(5),
               
            });
        }

        return result;
    }

    //Список категорий с id
    public IEnumerable<Category> GetAllCategories()
    {
        using var conn = _db.CreateConnection();

        var query = @"
        SELECT Id, Name
        FROM Categories
        ORDER BY Name
    ";

        using var command = conn.CreateCommand();
        command.CommandText = query;

        using var reader = command.ExecuteReader();

        var result = new List<Category>();

        while (reader.Read())
        {
            result.Add(new Category
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            });
        }

        return result;
    }

    //Обновление категории приложения
    public void UpdateApplicationCategory(int applicationId, int newCategoryId)
    {
        using var conn = _db.CreateConnection();

        var query = @"
        UPDATE Applications
        SET CategoryId = @categoryId
        WHERE Id = @appId
        ";

        using var command = conn.CreateCommand();
        command.CommandText = query;

        command.Parameters.AddWithValue("@categoryId", newCategoryId);
        command.Parameters.AddWithValue("@appId", applicationId);

        command.ExecuteNonQuery();
    }
}
