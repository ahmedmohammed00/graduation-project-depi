using System;
using System.Collections.Generic;

namespace FastDrop.MVC.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; } = 0;

        public int? MerchantId { get; set; }
        public Merchant? Merchant { get; set; }

        public ICollection<ProductWarehouse> ProductWarehouses { get; set; } = new List<ProductWarehouse>();
        public ICollection<ShipmentProduct> ShipmentProducts { get; set; } = new List<ShipmentProduct>();
    }
}
