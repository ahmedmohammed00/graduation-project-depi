using System;
using System.Collections.Generic;

namespace FastDrop.MVC.DTOs
{
    public class FeedbackDto
    {
        public int FeedbackId { get; set; }
        public int? ShipmentId { get; set; }
        public int? DriverId { get; set; }
        public decimal? Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
