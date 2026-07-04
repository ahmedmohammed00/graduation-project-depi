using System;
using System.Collections.Generic;

namespace FastDrop.MVC.DTOs
{
    public class PerformanceScoreDto
    {
        public int PerfId { get; set; }
        public int? DriverId { get; set; }
        public decimal? DailyScore { get; set; }
        public string? Tier { get; set; }
        public decimal? WeightedHistory { get; set; }
        public DateTime RecordedAt { get; set; }
    }
}
