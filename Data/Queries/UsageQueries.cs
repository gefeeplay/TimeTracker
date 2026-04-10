using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeTracker.Models;
using Dapper;

namespace TimeTracker.Data.Queries
{
    public class UsageQueries
    {
        private readonly Database _db;

        public UsageQueries(Database db)
        {
            _db = db;
        }

        public void InsertSession(UsageSession session)
        {
            _db.Execute(@"
            INSERT INTO UsageSessions
            (ApplicationId, StartTime, EndTime, DurationSeconds, CreatedAt)
            VALUES (@ApplicationId, @StartTime, @EndTime, @DurationSeconds, @CreatedAt)",
                session);
        }

        public Application? GetApplicationByProcess(string processName)
        {
            return _db.QuerySingleOrDefault<Application>(
                "SELECT * FROM Applications WHERE ProcessName = @name",
                new { name = processName });
        }

        public int InsertApplication(string processName)
        {
            return _db.QuerySingle<int>(@"
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
        }
    }
}
