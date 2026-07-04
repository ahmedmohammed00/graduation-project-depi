using System;
using System.Collections.Generic;

namespace FastDrop.MVC.Models
{
    public class DriverPayment
    {
        public int PaymentId { get; set; }
        
        public int? DriverId { get; set; }
        public Driver? Driver { get; set; }

        public decimal? TotalPayout { get; set; } = 0.00m;
        public decimal? DistanceKm { get; set; } = 0.00m;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
