using System;
using System.Collections.Generic;

namespace FastDrop.MVC.DTOs
{
    public class ShipmentAttemptDto
    {
        public int AttemptId { get; set; }
        public int ShipmentId { get; set; }
        public int? DriverId { get; set; }
        public int AttemptNumber { get; set; }
        public string? Status { get; set; }
        public string? OtpCode { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Reason { get; set; }
        public DateTime AttemptTime { get; set; }
    }
}
