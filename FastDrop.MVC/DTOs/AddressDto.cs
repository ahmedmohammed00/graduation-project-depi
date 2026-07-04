using System;
using System.Collections.Generic;

namespace FastDrop.MVC.DTOs
{
    public class AddressDto
    {
        public int AddressId { get; set; }
        public string? District { get; set; }
        public string? Street { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int? ZoneId { get; set; }
    }
}
