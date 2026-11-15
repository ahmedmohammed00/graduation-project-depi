using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FastDrop.MVC.ViewModels;

// Request models deliberately contain no EF entities, owner IDs, assignments or status graphs.
public class AddShipmentViewModel
{
    [Required]
    public MerchantShipmentInput Shipment { get; set; } = new();
    [Required]
    public List<SelectedProduct> SelectedProducts { get; set; } = new();
}

public class MerchantShipmentInput
{
    [Required]
    public MerchantAddressInput Address { get; set; } = new();
    [Range(typeof(decimal), "0.01", "999999.99")]
    public decimal? WeightKg { get; set; }
}

public class MerchantAddressInput
{
    [Required, StringLength(200)]
    public string Street { get; set; } = "";
    [Required, StringLength(100)]
    public string District { get; set; } = "";
    [Range(1, int.MaxValue)]
    public int ZoneId { get; set; }
}

public class SelectedProduct
{
    public int ProductId { get; set; }
    public bool IsSelected { get; set; }
    public int Quantity { get; set; } = 1;
    [BindNever, ValidateNever]
    public string ProductName { get; set; } = "";
    [BindNever, ValidateNever]
    public decimal Price { get; set; }
    [BindNever, ValidateNever]
    public int StockQuantity { get; set; }
}

public class MerchantShipmentEditViewModel : MerchantShipmentInput
{
    [BindNever]
    public int ShipmentId { get; set; }
}

public class MerchantProductEditViewModel
{
    [BindNever]
    public int ProductId { get; set; }
    [Required, StringLength(200)]
    public string Name { get; set; } = "";
    [StringLength(2000)]
    public string? Description { get; set; }
    [Range(typeof(decimal), "0", "99999999.99")]
    public decimal Price { get; set; }
    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }
    [Required, Range(0, int.MaxValue)]
    public int? OriginalStockQuantity { get; set; }
}
