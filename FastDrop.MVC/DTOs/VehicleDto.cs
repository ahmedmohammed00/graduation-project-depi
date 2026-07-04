using System;
using System.Collections.Generic;

namespace FastDrop.MVC.DTOs
{
    public class VehicleDto
    {
        public int VehicleId { get; set; }
        public string LicensePlate { get; set; }
        public string? Model { get; set; }
        public string? VehicleType { get; set; }
        public decimal? MaxCapacityKg { get; set; }
        public DateTime? LicenseExpiryDate { get; set; }
    }
}
