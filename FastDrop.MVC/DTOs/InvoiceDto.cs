using System;
using System.Collections.Generic;

namespace FastDrop.MVC.DTOs
{
    public class InvoiceDto
    {
        public int InvoiceId { get; set; }
        public int? ShipmentId { get; set; }
        public int? MerchantId { get; set; }
        public decimal AmountDue { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime? PeriodStart { get; set; }
        public DateTime? PeriodEnd { get; set; }
    }
}
