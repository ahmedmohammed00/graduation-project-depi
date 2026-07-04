using System;
using System.Collections.Generic;

namespace FastDrop.MVC.DTOs
{
    public class RouteStopsDto
    {
        public int StopId { get; set; }
        public int RouteId { get; set; }
        public int AddressId { get; set; }
        public int SequenceOrder { get; set; }
        public DateTime? DeliveredAt { get; set; }
    }
}
