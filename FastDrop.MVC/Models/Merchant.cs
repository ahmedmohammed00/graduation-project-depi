using System;
using System.Collections.Generic;

namespace FastDrop.MVC.Models
{
    public class Merchant
    {
        public int MerchantId { get; set; }
        public string ShopName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? ContactName { get; set; }
        public string? Phone { get; set; }
        public string Status { get; set; } = "active";
        public Guid SecurityStamp { get; set; } = Guid.NewGuid();

        public int? AddressId { get; set; }
        public Address? Address { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}
