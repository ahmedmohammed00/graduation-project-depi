using System;
using System.Collections.Generic;

namespace FastDrop.MVC.DTOs
{
    public class PodEvidenceDto
    {
        public int AttemptId { get; set; }
        public string? PhotoUrl { get; set; }
        public bool OtpVerified { get; set; }
        public string? Notes { get; set; }
    }
}
