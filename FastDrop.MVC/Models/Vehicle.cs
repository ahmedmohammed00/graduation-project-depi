using System;
using System.Collections.Generic;

namespace FastDrop.MVC.Models
{
    public class Vehicle
    {
        public int VehicleId { get; set; }
        public string LicensePlate { get; set; } = null!;
        public string? Model { get; set; }
        public string? VehicleType { get; set; }
        public decimal? MaxCapacityKg { get; set; }
        public DateTime? LicenseExpiryDate { get; set; }

        public ICollection<DriverVehicle> DriverVehicles { get; set; } = new List<DriverVehicle>();
    }
}
