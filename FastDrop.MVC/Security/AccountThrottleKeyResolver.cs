using FastDrop.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace FastDrop.MVC.Security;

public interface IAccountThrottleKeyResolver
{
    Task<string> ResolveAsync(string email, CancellationToken cancellationToken);
}

public class AccountThrottleKeyResolver(DmsDbContext context) : IAccountThrottleKeyResolver
{
    public async Task<string> ResolveAsync(string email, CancellationToken cancellationToken)
    {
        // Match login's database collation and employee-first lookup. Email aliases/case/accent
        // variants resolving to the same account must share its request budget.
        var employee = await context.Employees.AsNoTracking().Where(e => e.Email == email)
            .Select(e => (int?)e.EmployeeId).FirstOrDefaultAsync(cancellationToken);
        if (employee.HasValue) return "employee:" + employee.Value;
        var merchant = await context.Merchants.AsNoTracking().Where(m => m.Email == email)
            .Select(m => (int?)m.MerchantId).FirstOrDefaultAsync(cancellationToken);
        return merchant.HasValue ? "merchant:" + merchant.Value : "email:" + email.ToUpperInvariant();
    }
}
