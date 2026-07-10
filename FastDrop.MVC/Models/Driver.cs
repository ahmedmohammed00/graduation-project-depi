using System;
using System.Collections.Generic;

namespace FastDrop.MVC.Models
{
    public class Driver
    {
        public int DriverId { get; set; }
        public string Name { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? LicenseNumber { get; set; }
        public DateTime? LicenseExpiryDate { get; set; }
        public string CurrentStatus { get; set; } = "available";

        public int? ZoneId { get; set; }
        public Zone? Zone { get; set; }

        public int? AddressId { get; set; }
        public Address? Address { get; set; }

        public int? EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public ICollection<DriverVehicle> DriverVehicles { get; set; } = new List<DriverVehicle>();
        public ICollection<DriverRoute> DriverRoutes { get; set; } = new List<DriverRoute>();
        public ICollection<Shipment> DirectShipments { get; set; } = new List<Shipment>();
        public ICollection<ShipmentAttempt> ShipmentAttempts { get; set; } = new List<ShipmentAttempt>();
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
        public ICollection<DriverPayment> Payments { get; set; } = new List<DriverPayment>();
        public ICollection<PerformanceScore> PerformanceScores { get; set; } = new List<PerformanceScore>();
    }
}
