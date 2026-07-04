using System;
using System.Collections.Generic;

namespace FastDrop.MVC.Models
{
    public class RouteStops
    {
        public int StopId { get; set; }
        
        public int RouteId { get; set; }
        public Route Route { get; set; } = null!;

        public int AddressId { get; set; }
        public Address Address { get; set; } = null!;

        public int SequenceOrder { get; set; }
        public DateTime? DeliveredAt { get; set; }
    }
}
