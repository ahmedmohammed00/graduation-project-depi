using System;
using System.Collections.Generic;

namespace FastDrop.MVC.Models
{
    public class DriverVehicle
    {
        public int DriverId { get; set; }
        public Driver Driver { get; set; } = null!;

        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; } = null!;
    }
}
