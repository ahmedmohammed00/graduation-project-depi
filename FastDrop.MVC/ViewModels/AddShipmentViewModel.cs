using System.Collections.Generic;
using FastDrop.MVC.Models;

namespace FastDrop.MVC.ViewModels
{
    public class AddShipmentViewModel
    {
        public Shipment Shipment { get; set; } = new Shipment();
        public List<SelectedProduct> SelectedProducts { get; set; } = new List<SelectedProduct>();
    }

    public class SelectedProduct
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public bool IsSelected { get; set; }
        public int Quantity { get; set; } = 1;
    }
}
