using System;
using System.Collections.Generic;

namespace FastDrop.MVC.Models
{
    public class Feedback
    {
        public int FeedbackId { get; set; }
        
        public int? ShipmentId { get; set; }
        public Shipment? Shipment { get; set; }

        public int? DriverId { get; set; }
        public Driver? Driver { get; set; }

        public decimal? Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
