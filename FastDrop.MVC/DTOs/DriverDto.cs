using System;
using System.Collections.Generic;

namespace FastDrop.MVC.DTOs
{
    public class DriverDto
    {
        public int DriverId { get; set; }
        public string Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? LicenseNumber { get; set; }
        public DateTime? LicenseExpiryDate { get; set; }
        public string CurrentStatus { get; set; }
        public int? ZoneId { get; set; }
        public int? AddressId { get; set; }
        public int? EmployeeId { get; set; }
    }
}
