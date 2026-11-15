using System.Security.Claims;
using FastDrop.MVC.ViewModels;

namespace FastDrop.MVC.Services;

public interface IProfileService
{
    Task<ProfileViewModel?> GetAsync(ClaimsPrincipal user);
    Task<ProfileSaveResult> UpdateAsync(ClaimsPrincipal user, ProfileEditViewModel model);
    Task<ProfileSaveResult> ChangePasswordAsync(ClaimsPrincipal user, ChangePasswordViewModel model);
}

public record ProfileSaveResult(bool Found, string? Error = null, string? Field = null,
    bool RequiresSignIn = false, string? DisplayName = null);
