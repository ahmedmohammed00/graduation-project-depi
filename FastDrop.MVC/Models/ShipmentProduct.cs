using System;
using System.Collections.Generic;

namespace FastDrop.MVC.Models
{
    public class ShipmentProduct
    {
        public int ShipmentId { get; set; }
        public Shipment Shipment { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int Quantity { get; set; } = 1;
        // Historical shipments predate price capture; never invent a price for those rows.
        public decimal? UnitPrice { get; set; }
    }
}
