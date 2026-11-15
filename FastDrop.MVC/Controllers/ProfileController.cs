using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FastDrop.MVC.Security;
using FastDrop.MVC.Services;
using FastDrop.MVC.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace FastDrop.MVC.Controllers;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ProfileController(IProfileService profiles, JwtSettings settings) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var profile = await profiles.GetAsync(User);
        return profile == null ? NotFound() : View(profile);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind(Prefix = "Details")] ProfileEditViewModel model)
    {
        if (!ModelState.IsValid) return await InvalidAsync("Details");
        var result = await profiles.UpdateAsync(User, model);
        if (!result.Found) return NotFound();
        if (result.Error != null)
        {
            ModelState.AddModelError(Field("Details", result.Field), result.Error);
            return await InvalidAsync("Details");
        }
        if (result.RequiresSignIn) return SignInAgain("Email updated. Sign in with your new email address.");
        RefreshDisplayName(result.DisplayName!);
        TempData["SuccessMessage"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = "Password")] ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return await InvalidAsync("Password");
        var result = await profiles.ChangePasswordAsync(User, model);
        if (!result.Found) return NotFound();
        if (result.Error != null)
        {
            ModelState.AddModelError(Field("Password", result.Field), result.Error);
            return await InvalidAsync("Password");
        }
        return SignInAgain("Password updated. Sign in with your new password.");
    }

    private async Task<IActionResult> InvalidAsync(string section)
    {
        // Passwords must never be rendered back into either form after an error.
        // Keep validation messages while discarding attempted secret values.
        foreach (var field in new[] { "Details.CurrentPassword", "Password.CurrentPassword", "Password.NewPassword", "Password.ConfirmPassword" })
            if (ModelState.TryGetValue(field, out var state)) state.RawValue = state.AttemptedValue = null;
        var profile = await profiles.GetAsync(User);
        if (profile == null) return NotFound();
        ViewData["ProfileErrorSection"] = section;
        return View("Index", profile);
    }

    private IActionResult SignInAgain(string message)
    {
        Response.Cookies.Delete("jwt");
        TempData["ProfileSignInMessage"] = message;
        return RedirectToAction("Login", "Account");
    }

    private void RefreshDisplayName(string name)
    {
        // A display-name edit must not extend the original session lifetime or change its roles.
        if (!long.TryParse(User.FindFirstValue("exp"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var expiry)) return;
        var expires = DateTimeOffset.FromUnixTimeSeconds(expiry).UtcDateTime;
        if (expires <= DateTime.UtcNow) return;
        var claims = User.Claims.Where(c => c.Type is not ("exp" or "nbf" or "iat" or "iss" or "aud") && c.Type != ClaimTypes.Name)
            .Append(new Claim(ClaimTypes.Name, name));
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims, expires: expires,
            signingCredentials: new SigningCredentials(settings.SigningKey, SecurityAlgorithms.HmacSha256));
        Response.Cookies.Append("jwt", new JwtSecurityTokenHandler().WriteToken(token), new CookieOptions
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = expires
        });
    }

    private static string Field(string prefix, string? field) => field == null ? prefix : $"{prefix}.{field}";
}
