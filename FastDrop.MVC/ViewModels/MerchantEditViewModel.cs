using System.ComponentModel.DataAnnotations;
using FastDrop.MVC.Models;

namespace FastDrop.MVC.ViewModels;

public class MerchantEditViewModel
{
    public int MerchantId { get; set; }
    [Required]
    public string ShopName { get; set; } = null!;
    [Required, EmailAddress]
    public string Email { get; set; } = null!;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    [Required]
    public string Status { get; set; } = "active";
    public int? AddressId { get; set; }

    public static MerchantEditViewModel FromMerchant(Merchant merchant) => new()
    {
        MerchantId = merchant.MerchantId, ShopName = merchant.ShopName, Email = merchant.Email,
        ContactName = merchant.ContactName, Phone = merchant.Phone, Status = merchant.Status,
        AddressId = merchant.AddressId
    };
}
