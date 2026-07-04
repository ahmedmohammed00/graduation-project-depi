using System;
using System.Collections.Generic;

namespace FastDrop.MVC.Models
{
    public class ShipmentStatus
    {
        public int StatusId { get; set; }
        
        public int ShipmentId { get; set; }
        public Shipment Shipment { get; set; } = null!;

        public string Status { get; set; } = null!;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
