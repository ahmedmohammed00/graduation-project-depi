using System.Security.Claims;
using FastDrop.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace FastDrop.MVC.Security;

public class AccountSessionValidator(DmsDbContext context) : IAccountSessionValidator
{
    public const string StampClaim = "SecurityStamp";
    public const string TypeClaim = "AccountType";

    public async Task<bool> ValidateAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!Identity(user, out var id, out var type, out var stamp)) return false;
        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        if (roles.Length == 0 || roles.Distinct(StringComparer.Ordinal).Count() != roles.Length) return false;
        if (type == "Merchant")
        {
            if (roles.Length != 1 || roles[0] != "Merchant" || ClaimId(user, "MerchantId") != id ||
                user.HasClaim(c => c.Type == "DriverId")) return false;
            var merchant = await context.Merchants.AsNoTracking().Where(m => m.MerchantId == id)
                .Select(m => new { m.SecurityStamp, m.Status }).SingleOrDefaultAsync(cancellationToken);
            return merchant != null && merchant.SecurityStamp == stamp && Active(merchant.Status);
        }
        if (type != "Employee" || user.HasClaim(c => c.Type == "MerchantId")) return false;
        var employee = await context.Employees.AsNoTracking().Where(e => e.EmployeeId == id)
            .Select(e => new { e.SecurityStamp, e.AccountStatus, Role = e.Role == null ? null : e.Role.RoleName })
            .SingleOrDefaultAsync(cancellationToken);
        if (employee == null || employee.SecurityStamp != stamp || !Active(employee.AccountStatus)) return false;
        var drivers = await context.Drivers.AsNoTracking().Where(d => d.EmployeeId == id)
            .Select(d => d.DriverId).Take(2).ToListAsync(cancellationToken);
        if (drivers.Count > 1) return false;
        var role = string.IsNullOrWhiteSpace(employee.Role) && drivers.Count == 1 ? "Driver" : employee.Role;
        if (role is not ("SuperAdmin" or "Dispatcher" or "Employee" or "Driver") || (role == "Driver" && drivers.Count == 0)) return false;
        var expected = new HashSet<string>(StringComparer.Ordinal) { role };
        if (drivers.Count == 1)
        {
            if (ClaimId(user, "DriverId") != drivers[0]) return false;
            expected.Add("Driver");
        }
        else if (user.HasClaim(c => c.Type == "DriverId")) return false;
        return expected.SetEquals(roles);
    }

    public async Task RevokeAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!Identity(user, out var id, out var type, out var stamp)) return;
        if (type == "Merchant")
        {
            var merchant = await context.Merchants.SingleOrDefaultAsync(m => m.MerchantId == id && m.SecurityStamp == stamp, cancellationToken);
            if (merchant != null) merchant.SecurityStamp = Guid.NewGuid();
        }
        else if (type == "Employee")
        {
            var employee = await context.Employees.SingleOrDefaultAsync(e => e.EmployeeId == id && e.SecurityStamp == stamp, cancellationToken);
            if (employee != null) employee.SecurityStamp = Guid.NewGuid();
        }
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            // Another credential/session change has already revoked this stamp.
            context.ChangeTracker.Clear();
        }
    }

    private static bool Identity(ClaimsPrincipal user, out int id, out string? type, out Guid stamp)
    {
        id = ClaimId(user, ClaimTypes.NameIdentifier) ?? 0;
        type = Single(user, TypeClaim);
        stamp = Guid.Empty;
        return user.Identity?.IsAuthenticated == true && id > 0 &&
            Guid.TryParse(Single(user, StampClaim), out stamp) && stamp != Guid.Empty && type is "Employee" or "Merchant";
    }
    private static string? Single(ClaimsPrincipal user, string type)
    {
        var claims = user.FindAll(type).ToArray();
        return claims.Length == 1 ? claims[0].Value : null;
    }
    private static int? ClaimId(ClaimsPrincipal user, string type) => int.TryParse(Single(user, type), out var id) && id > 0 ? id : null;
    internal static bool Active(string? status) => string.Equals(status, "active", StringComparison.OrdinalIgnoreCase);
}
