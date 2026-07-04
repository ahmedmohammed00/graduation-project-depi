using System;
using System.Collections.Generic;

namespace FastDrop.MVC.DTOs
{
    public class WarehouseDto
    {
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public int? AddressId { get; set; }
    }
}
