using System;
using System.Collections.Generic;

namespace FastDrop.MVC.Models
{
    public class Zone
    {
        public int ZoneId { get; set; }
        public string Name { get; set; } = null!;
        public string? City { get; set; }
        public string? GeojsonBoundary { get; set; }
        public decimal? DeliveryFee { get; set; } = 0.00m;
        public bool ActiveStatus { get; set; } = true;

        public ICollection<Address> Addresses { get; set; } = new List<Address>();
        public ICollection<Driver> Drivers { get; set; } = new List<Driver>();
    }
}
