using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeTracker.Models;

namespace TimeTracker.Data.Queries
{
    public class StatisticsQueries
    {
        private readonly Database _db;

        public StatisticsQueries(Database db)
        {
            _db = db;
        }

        public IEnumerable<DailyAppStat> GetStatsByDate(DateTime date)
        {
            return _db.Query<DailyAppStat>(@"
            SELECT * FROM DailyAppStats
            WHERE Date = @date
            ORDER BY TotalDurationSeconds DESC",
                new { date = date.Date });
        }

        public IEnumerable<UsageSession> GetSessions(DateTime from, DateTime to)
        {
            return _db.Query<UsageSession>(@"
            SELECT * FROM UsageSessions
            WHERE StartTime >= @from AND EndTime <= @to",
                new { from, to });
        }

        public IEnumerable<(string AppName, int TotalSeconds)> GetTopApps(DateTime from, DateTime to)
        {
            using var conn = _db.CreateConnection();

            var result = conn.Query(@"
            SELECT a.DisplayName as AppName,
                   SUM(u.DurationSeconds) as TotalSeconds
            FROM UsageSessions u
            JOIN Applications a ON a.Id = u.ApplicationId
            WHERE u.StartTime >= @from AND u.EndTime <= @to
            GROUP BY a.DisplayName
            ORDER BY TotalSeconds DESC",
                new { from, to });

            return result.Select(x => ((string)x.AppName, (int)x.TotalSeconds));
        }

        public void RecalculateDailyStats(DateTime date)
        {
            _db.Execute(@"
            INSERT INTO DailyAppStats (ApplicationId, Date, TotalDurationSeconds, SessionsCount, UpdatedAt)
            SELECT 
                ApplicationId,
                DATE(StartTime),
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
                    date = date.Date,
                    now = DateTime.Now
                });
        }
    }
}
