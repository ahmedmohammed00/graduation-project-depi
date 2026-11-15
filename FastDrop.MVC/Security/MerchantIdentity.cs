using System.Security.Claims;

namespace FastDrop.MVC.Security;

public static class MerchantIdentity
{
    public static int? GetId(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true || !user.IsInRole("Merchant") ||
            user.FindAll(ClaimTypes.Role).Count() != 1 || user.HasClaim(c => c.Type == "DriverId")) return null;
        static int? Id(ClaimsPrincipal principal, string type)
        {
            var claims = principal.FindAll(type).ToArray();
            return claims.Length == 1 && int.TryParse(claims[0].Value, out var id) && id > 0 ? id : null;
        }
        var merchantId = Id(user, "MerchantId");
        return merchantId.HasValue && merchantId == Id(user, ClaimTypes.NameIdentifier) ? merchantId : null;
    }
}
