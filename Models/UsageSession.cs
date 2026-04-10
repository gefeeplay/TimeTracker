using System;

namespace TimeTracker.Models 
{
    public class UsageSession
    {
        public int Id { get; set; }

        public int ApplicationId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public int DurationSeconds { get; set; }

        public DateTime CreatedAt { get; set; }

        // 🔹 Удобное вычисляемое свойство (не из БД)
        public TimeSpan Duration => TimeSpan.FromSeconds(DurationSeconds);
    }
}

