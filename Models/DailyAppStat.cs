using System;

namespace TimeTracker.Models
{
    public class DailyAppStat
    {
        public int Id { get; set; }

        public int ApplicationId { get; set; }

        public DateTime Date { get; set; }

        public int TotalDurationSeconds { get; set; }

        public int SessionsCount { get; set; }

        public DateTime UpdatedAt { get; set; }

        // 🔹 Удобное свойство для UI
        public TimeSpan TotalDuration => TimeSpan.FromSeconds(TotalDurationSeconds);
    }
}
