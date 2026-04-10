using Dapper;
using System;
using TimeTracker.Data;

namespace TimeTracker.Services;

public class CleanupService
{
    private readonly Database _db;

    public CleanupService(Database db)
    {
        _db = db;
    }

    public void RunCleanup()
    {
        using var conn = _db.CreateConnection();

        var sessionsBorder = DateTime.Now.AddMonths(-6);
        var statsBorder = DateTime.Now.AddMonths(-12);

        // 🧹 Удаление старых сессий
        conn.Execute(@"
            DELETE FROM UsageSessions
            WHERE StartTime < @border",
            new { border = sessionsBorder });

        // 🧹 Удаление старой агрегированной статистики
        conn.Execute(@"
            DELETE FROM DailyAppStats
            WHERE Date < @border",
            new { border = statsBorder.Date });
    }
}