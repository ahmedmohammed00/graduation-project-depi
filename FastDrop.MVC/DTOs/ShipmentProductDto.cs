using System;
using System.Collections.Generic;

namespace FastDrop.MVC.DTOs
{
    public class ShipmentProductDto
    {
        public int ShipmentId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
