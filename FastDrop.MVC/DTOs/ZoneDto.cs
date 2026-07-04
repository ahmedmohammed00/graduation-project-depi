using System;
using System.Collections.Generic;

namespace FastDrop.MVC.DTOs
{
    public class ZoneDto
    {
        public int ZoneId { get; set; }
        public string Name { get; set; }
        public string? City { get; set; }
        public string? GeojsonBoundary { get; set; }
        public decimal? DeliveryFee { get; set; }
        public bool ActiveStatus { get; set; }
    }
}
