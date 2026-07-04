using System;
using System.Collections.Generic;

namespace FastDrop.MVC.Models
{
    public class Invoice
    {
        public int InvoiceId { get; set; }
        
        public int? ShipmentId { get; set; }
        public Shipment? Shipment { get; set; }

        public int? MerchantId { get; set; }
        public Merchant? Merchant { get; set; }

        public decimal AmountDue { get; set; }
        public string PaymentStatus { get; set; } = "pending";
        public DateTime? PeriodStart { get; set; }
        public DateTime? PeriodEnd { get; set; }
    }
}
