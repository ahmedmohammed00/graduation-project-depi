using System;
using System.Collections.Generic;

namespace FastDrop.MVC.DTOs
{
    public class ShipmentDto
    {
        public int ShipmentId { get; set; }
        public DateTime CreationDate { get; set; }
        public decimal CodAmount { get; set; }
        public decimal? WeightKg { get; set; }
        public int? WarehouseId { get; set; }
        public int? MerchantId { get; set; }
        public int? RouteId { get; set; }
        public int? AddressId { get; set; }
    }
}
