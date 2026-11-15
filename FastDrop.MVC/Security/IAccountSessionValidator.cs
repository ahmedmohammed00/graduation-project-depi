using System.Security.Claims;

namespace FastDrop.MVC.Security;

public interface IAccountSessionValidator
{
    Task<bool> ValidateAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task RevokeAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
}
