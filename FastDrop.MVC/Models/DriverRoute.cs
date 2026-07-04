using System;
using System.Collections.Generic;

namespace FastDrop.MVC.Models
{
    public class DriverRoute
    {
        public int DriverId { get; set; }
        public Driver Driver { get; set; } = null!;

        public int RouteId { get; set; }
        public Route Route { get; set; } = null!;

        public decimal? RoutePay { get; set; } = 0.00m;
    }
}
