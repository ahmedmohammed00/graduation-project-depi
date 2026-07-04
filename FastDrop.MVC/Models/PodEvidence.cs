using System;
using System.Collections.Generic;

namespace FastDrop.MVC.Models
{
    public class PodEvidence
    {
        public int AttemptId { get; set; }
        public ShipmentAttempt ShipmentAttempt { get; set; } = null!;

        public string? PhotoUrl { get; set; }
        public bool OtpVerified { get; set; } = false;
        public string? Notes { get; set; }
    }
}
