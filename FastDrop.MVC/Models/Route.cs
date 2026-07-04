using System;
using System.Collections.Generic;

namespace FastDrop.MVC.Models
{
    public class Route
    {
        public int RouteId { get; set; }
        public string? RouteName { get; set; }
        public string Status { get; set; } = "active";

        public ICollection<RouteStops> RouteStops { get; set; } = new List<RouteStops>();
        public ICollection<DriverRoute> DriverRoutes { get; set; } = new List<DriverRoute>();
        public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    }
}
