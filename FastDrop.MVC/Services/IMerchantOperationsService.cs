using System.Security.Claims;
using FastDrop.MVC.ViewModels;

namespace FastDrop.MVC.Services;

public interface IMerchantOperationsService
{
    Task<MerchantShipmentEditViewModel?> GetShipmentEditAsync(int id, ClaimsPrincipal user);
    Task<MerchantProductEditViewModel?> GetProductEditAsync(int id, ClaimsPrincipal user);
    Task<MerchantWriteResult> CreateShipmentAsync(AddShipmentViewModel model, ClaimsPrincipal user);
    Task<MerchantWriteResult> EditShipmentAsync(int id, MerchantShipmentEditViewModel model, ClaimsPrincipal user);
    Task<MerchantWriteResult> EditProductAsync(int id, MerchantProductEditViewModel model, ClaimsPrincipal user);
}

public record MerchantWriteResult(int StatusCode, string? Error = null, int? ShipmentId = null)
{
    public static MerchantWriteResult NotFound => new(404);
    public static MerchantWriteResult Invalid(string message) => new(400, message);
}
