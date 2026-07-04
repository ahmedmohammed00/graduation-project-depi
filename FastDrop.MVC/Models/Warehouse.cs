using System;
using System.Collections.Generic;

namespace FastDrop.MVC.Models
{
    public class Warehouse
    {
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = null!;
        
        public int? AddressId { get; set; }
        public Address? Address { get; set; }

        public ICollection<ProductWarehouse> ProductWarehouses { get; set; } = new List<ProductWarehouse>();
        public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    }
}
