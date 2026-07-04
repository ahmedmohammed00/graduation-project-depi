using System;
using System.Collections.Generic;

namespace FastDrop.MVC.DTOs
{
    public class DriverPaymentDto
    {
        public int PaymentId { get; set; }
        public int? DriverId { get; set; }
        public decimal? TotalPayout { get; set; }
        public decimal? DistanceKm { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
