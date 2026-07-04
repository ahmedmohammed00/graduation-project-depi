using System;
using System.Collections.Generic;

namespace FastDrop.MVC.Models
{
    public class PerformanceScore
    {
        public int PerfId { get; set; }
        
        public int? DriverId { get; set; }
        public Driver? Driver { get; set; }

        public decimal? DailyScore { get; set; }
        public string? Tier { get; set; }
        public decimal? WeightedHistory { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}
