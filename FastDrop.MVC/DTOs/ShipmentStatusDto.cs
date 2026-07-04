using System;
using System.Collections.Generic;

namespace FastDrop.MVC.DTOs
{
    public class ShipmentStatusDto
    {
        public int StatusId { get; set; }
        public int ShipmentId { get; set; }
        public string Status { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
