using System;
using System.Collections.Generic;
using System.Text;

namespace TimeTracker.Models
{
    public class AiInsightsCache
    {
        public int Id { get; set; }

        public string Scope { get; set; } = "";

        public string Content { get; set; } = "";

        public DateTime UpdatedAt { get; set; }
    }
}
