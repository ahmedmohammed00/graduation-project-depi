using FastDrop.MVC.Models;
using FastDrop.MVC.Security;
using Microsoft.EntityFrameworkCore;

namespace FastDrop.MVC.Services;

public sealed class CredentialMigrationService(DmsDbContext context)
{
    public async Task<(int Employees, int Merchants, int RequiresReset)> MigrateLegacyPasswordsAsync(
        CancellationToken cancellationToken = default)
    {
        int employees = 0, merchants = 0, requiresReset = 0, lastId = 0;
        while (true)
        {
            var batch = await context.Employees.Where(e => e.EmployeeId > lastId)
                .OrderBy(e => e.EmployeeId).Take(100).ToListAsync(cancellationToken);
            if (batch.Count == 0) break;
            foreach (var employee in batch)
            {
                if (PasswordHashing.IsHash(employee.Password)) continue;
                if (LooksLikeDamagedHash(employee.Password)) { requiresReset++; continue; }
                employee.Password = PasswordHashing.HashLegacyPassword(employee.Password);
                employees++;
            }
            await context.SaveChangesAsync(cancellationToken);
            lastId = batch[^1].EmployeeId;
            context.ChangeTracker.Clear();
        }

        lastId = 0;
        while (true)
        {
            var batch = await context.Merchants.Where(m => m.MerchantId > lastId)
                .OrderBy(m => m.MerchantId).Take(100).ToListAsync(cancellationToken);
            if (batch.Count == 0) break;
            foreach (var merchant in batch)
            {
                if (PasswordHashing.IsHash(merchant.Password)) continue;
                if (LooksLikeDamagedHash(merchant.Password)) { requiresReset++; continue; }
                merchant.Password = PasswordHashing.HashLegacyPassword(merchant.Password);
                merchants++;
            }
            await context.SaveChangesAsync(cancellationToken);
            lastId = batch[^1].MerchantId;
            context.ChangeTracker.Clear();
        }

        return (employees, merchants, requiresReset);
    }

    // Never turn an unrecognized hash into a password that can be used to log in.
    private static bool LooksLikeDamagedHash(string password) =>
        password.StartsWith('$') || password.StartsWith(PasswordHashing.EnhancedPrefix, StringComparison.Ordinal);
}
