using System;
using System.Collections.Generic;

namespace FastDrop.MVC.Models
{
    public class Shipment
    {
        public int ShipmentId { get; set; }
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;
        public decimal CodAmount { get; set; } = 0.00m;
        public decimal? WeightKg { get; set; }

        public int? WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }

        public int? MerchantId { get; set; }
        public Merchant? Merchant { get; set; }

        public int? RouteId { get; set; }
        public Route? Route { get; set; }

        public int? DriverId { get; set; }
        public Driver? Driver { get; set; }

        public int? AddressId { get; set; }
        public Address? Address { get; set; }

        public ICollection<ShipmentStatus> ShipmentStatuses { get; set; } = new List<ShipmentStatus>();
        public ICollection<ShipmentProduct> ShipmentProducts { get; set; } = new List<ShipmentProduct>();
        public ICollection<ShipmentAttempt> ShipmentAttempts { get; set; } = new List<ShipmentAttempt>();
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    }
}
