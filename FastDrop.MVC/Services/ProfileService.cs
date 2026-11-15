using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FastDrop.MVC.Models;
using FastDrop.MVC.Security;
using FastDrop.MVC.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FastDrop.MVC.Services;

public class ProfileService(DmsDbContext context) : IProfileService
{
    private sealed record Account(Employee? Employee, Merchant? Merchant, Driver? Driver)
    {
        public string Hash => Merchant?.Password ?? Employee!.Password;
    }

    public async Task<ProfileViewModel?> GetAsync(ClaimsPrincipal user)
    {
        var account = await ResolveAsync(user);
        if (account == null) return null;
        var merchant = account.Merchant;
        return new ProfileViewModel
        {
            DisplayName = merchant?.ShopName ?? account.Employee!.Name,
            Role = merchant != null ? "Merchant" : account.Employee!.Role?.RoleName ?? "Driver",
            Status = merchant?.Status ?? account.Employee!.AccountStatus,
            IsMerchant = merchant != null,
            IsDriver = account.Driver != null,
            Details = new ProfileEditViewModel
            {
                Name = merchant?.ShopName ?? account.Employee!.Name,
                Email = merchant?.Email ?? account.Employee!.Email,
                ContactName = merchant?.ContactName,
                Phone = merchant?.Phone ?? account.Driver?.PhoneNumber
            }
        };
    }

    public async Task<ProfileSaveResult> UpdateAsync(ClaimsPrincipal user, ProfileEditViewModel model)
    {
        var account = await ResolveAsync(user);
        if (account == null) return new(false);
        model.Name = model.Name?.Trim() ?? "";
        model.Email = model.Email?.Trim() ?? "";
        var invalid = Validate(model);
        if (invalid != null) return invalid;

        var oldEmail = account.Merchant?.Email ?? account.Employee!.Email;
        var emailChanged = !string.Equals(oldEmail, model.Email, StringComparison.Ordinal);
        if (emailChanged && !PasswordHashing.Verify(model.CurrentPassword ?? "", account.Hash))
            return new(true, "Enter your current password to change your email address.", nameof(model.CurrentPassword));

        if (emailChanged)
        {
            var email = model.Email.ToUpperInvariant();
            var employeeId = account.Employee?.EmployeeId ?? 0;
            var merchantId = account.Merchant?.MerchantId ?? 0;
            if (await context.Employees.AnyAsync(e => e.EmployeeId != employeeId && e.Email.ToUpper() == email) ||
                await context.Merchants.AnyAsync(m => m.MerchantId != merchantId && m.Email.ToUpper() == email))
                return new(true, "This email address is already in use.", nameof(model.Email));
        }

        if (account.Merchant is Merchant merchant)
        {
            merchant.ShopName = model.Name;
            merchant.Email = model.Email;
            merchant.ContactName = Clean(model.ContactName);
            merchant.Phone = Clean(model.Phone);
        }
        else
        {
            account.Employee!.Name = model.Name;
            account.Employee.Email = model.Email;
            if (account.Driver is Driver driver)
            {
                driver.Name = model.Name;
                driver.PhoneNumber = Clean(model.Phone);
            }
        }
        return await SaveAsync(emailChanged, model.Name);
    }

    public async Task<ProfileSaveResult> ChangePasswordAsync(ClaimsPrincipal user, ChangePasswordViewModel model)
    {
        var account = await ResolveAsync(user);
        if (account == null) return new(false);
        var invalid = Validate(model);
        if (invalid != null) return invalid;
        if (!PasswordHashing.Verify(model.CurrentPassword, account.Hash))
            return new(true, "Your current password is incorrect.", nameof(model.CurrentPassword));
        if (PasswordHashing.Verify(model.NewPassword, account.Hash))
            return new(true, "Choose a password different from your current password.", nameof(model.NewPassword));
        if (string.IsNullOrWhiteSpace(model.NewPassword))
            return new(true, "Choose a password with 8 to 100 characters.", nameof(model.NewPassword));
        var hash = PasswordHashing.HashNewPassword(model.NewPassword);
        if (account.Merchant != null) account.Merchant.Password = hash;
        else account.Employee!.Password = hash;
        return await SaveAsync(true);
    }

    private async Task<ProfileSaveResult> SaveAsync(bool requiresSignIn, string? name = null)
    {
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            return new(true, "We couldn't save these changes. Please reload the page and try again.");
        }
        return new(true, RequiresSignIn: requiresSignIn, DisplayName: name);
    }

    private static ProfileSaveResult? Validate(object model)
    {
        var errors = new List<ValidationResult>();
        if (Validator.TryValidateObject(model, new ValidationContext(model), errors, true)) return null;
        return new(true, errors[0].ErrorMessage, errors[0].MemberNames.FirstOrDefault());
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<Account?> ResolveAsync(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true) return null;
        static int? Id(ClaimsPrincipal principal, string type)
        {
            var claims = principal.FindAll(type).ToArray();
            return claims.Length == 1 && int.TryParse(claims[0].Value, out var id) && id > 0 ? id : null;
        }
        var id = Id(user, ClaimTypes.NameIdentifier);
        if (id == null) return null;
        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        if (roles.Contains("Merchant"))
        {
            if (roles.Length != 1 || Id(user, "MerchantId") != id || user.HasClaim(c => c.Type == "DriverId")) return null;
            var merchant = await context.Merchants.SingleOrDefaultAsync(m => m.MerchantId == id);
            return merchant == null ? null : new(null, merchant, null);
        }
        if (roles.Length == 0 || roles.Any(r => r is not ("SuperAdmin" or "Dispatcher" or "Employee" or "Driver")) ||
            user.HasClaim(c => c.Type == "MerchantId")) return null;
        var employee = await context.Employees.Include(e => e.Role).SingleOrDefaultAsync(e => e.EmployeeId == id);
        if (employee == null) return null;
        var drivers = await context.Drivers.Where(d => d.EmployeeId == id).Take(2).ToListAsync();
        if (drivers.Count > 1) return null;
        var driver = drivers.SingleOrDefault();
        if (user.HasClaim(c => c.Type == "DriverId") && Id(user, "DriverId") != driver?.DriverId) return null;
        if (roles.Contains("Driver") && driver == null) return null;
        return new(employee, null, driver);
    }
}
