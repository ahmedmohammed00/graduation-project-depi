using System;
using System.Collections.Generic;

namespace FastDrop.MVC.DTOs
{
    public class DriverRouteDto
    {
        public int DriverId { get; set; }
        public int RouteId { get; set; }
        public decimal? RoutePay { get; set; }
    }
}
