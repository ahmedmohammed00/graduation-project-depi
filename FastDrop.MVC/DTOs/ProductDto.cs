using System;
using System.Collections.Generic;

namespace FastDrop.MVC.DTOs
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int? MerchantId { get; set; }
    }
}
