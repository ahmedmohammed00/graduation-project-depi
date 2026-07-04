using System;
using System.Collections.Generic;

namespace FastDrop.MVC.Models
{
    public class Address
    {
        public int AddressId { get; set; }
        public string? District { get; set; }
        public string? Street { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        
        public int? ZoneId { get; set; }
        public Zone? Zone { get; set; }

        public ICollection<Merchant> Merchants { get; set; } = new List<Merchant>();
        public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
        public ICollection<Driver> Drivers { get; set; } = new List<Driver>();
        public ICollection<RouteStops> RouteStops { get; set; } = new List<RouteStops>();
        public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    }
}
