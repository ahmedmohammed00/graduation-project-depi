using System;
using System.Collections.Generic;

namespace FastDrop.MVC.Models
{
    public class ProductWarehouse
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; } = null!;

        public int Quantity { get; set; } = 0;
    }
}
