using System;

namespace TimeTracker.Models
{
    public class Application
    {
        public int Id { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public int CategoryId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
