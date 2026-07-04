using System;
using System.Collections.Generic;

namespace FastDrop.MVC.DTOs
{
    public class MerchantDto
    {
        public int MerchantId { get; set; }
        public string ShopName { get; set; }
        public string Email { get; set; }
        public string? ContactName { get; set; }
        public string? Phone { get; set; }
        public string Status { get; set; }
        public int? AddressId { get; set; }
    }
}
