using System.Security.Claims;
using FastDrop.MVC.Models;
using FastDrop.MVC.Security;
using FastDrop.MVC.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;
using TestDatabase = FastDrop.MVC.Tests.MerchantWriteSqlTests.TestDatabase;

namespace FastDrop.MVC.Tests;

public class DeliveryAndSessionSqlTests
{
    [LocalMerchantSqlFact]
    public async Task ConcurrentDeliveryRetriesCreateOneAtomicDeliveryAndPayout()
    {
        await using var database = await TestDatabase.CreateAsync();
        var ids = await PendingDelivery(database);
        var gate = new TwoDeliveryWriters();
        await using var first = database.Context(gate);
        await using var second = database.Context(gate);
        var results = await Task.WhenAll(
            new ShipmentService(first).SubmitPodAsync(ids.Shipment, ids.Driver, "simulation", "first"),
            new ShipmentService(second).SubmitPodAsync(ids.Shipment, ids.Driver, "simulation", "retry"));
        Assert.All(results, result => Assert.True(result.IsSuccess, result.Message));
        await using var check = database.Context();
        Assert.Equal(1, await check.DriverPayments.CountAsync());
        Assert.Equal(15m, (await check.DriverPayments.SingleAsync()).TotalPayout);
        Assert.Equal(1, await check.ShipmentAttempts.CountAsync());
        Assert.Equal(1, await check.PodEvidences.CountAsync());
        Assert.Equal(1, await check.ShipmentStatuses.CountAsync(s => s.Status == "Delivered"));
        Assert.NotNull((await check.Shipments.SingleAsync()).DeliveryConfirmedAt);
        Assert.Null((await check.ShipmentAttempts.SingleAsync()).OtpCode);
        var retry = await new ShipmentService(check).SubmitPodAsync(ids.Shipment, ids.Driver, "simulation", null);
        Assert.True(retry.IsSuccess); Assert.Equal(1, await check.DriverPayments.CountAsync());
        var reopened = await new ShipmentService(check).UpdateStatusAsync(ids.Shipment, "Pending", null, "Driver", ids.Driver);
        Assert.False(reopened.IsSuccess);
    }

    [LocalMerchantSqlFact]
    public async Task PaymentFailureRollsBackTheDeliveryMarkerStatusAttemptAndEvidence()
    {
        await using var database = await TestDatabase.CreateAsync();
        var ids = await PendingDelivery(database);
        await using (var schema = database.Context())
            await schema.Database.ExecuteSqlRawAsync("ALTER TABLE [Driver_Payment] ADD CONSTRAINT [CK_Test_RejectPayout] CHECK ([TotalPayout] < 0)");
        await using var db = database.Context();
        var result = await new ShipmentService(db).SubmitPodAsync(ids.Shipment, ids.Driver, "simulation", null);
        Assert.False(result.IsSuccess);
        await using var check = database.Context();
        Assert.Null((await check.Shipments.SingleAsync()).DeliveryConfirmedAt);
        Assert.Empty(await check.DriverPayments.ToListAsync()); Assert.Empty(await check.ShipmentAttempts.ToListAsync());
        Assert.Empty(await check.PodEvidences.ToListAsync());
        Assert.Equal(0, await check.ShipmentStatuses.CountAsync(s => s.Status == "Delivered"));
    }

    [LocalMerchantSqlFact]
    public async Task UniquePayoutIndexRejectsAnIndependentDuplicateInsert()
    {
        await using var database = await TestDatabase.CreateAsync();
        var ids = await PendingDelivery(database);
        await using var db = database.Context();
        Assert.True((await new ShipmentService(db).SubmitPodAsync(ids.Shipment, ids.Driver, "simulation", null)).IsSuccess);
        db.DriverPayments.Add(new DriverPayment { ShipmentId = ids.Shipment, DriverId = ids.Driver, TotalPayout = 15 });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear(); Assert.Equal(1, await db.DriverPayments.CountAsync());
    }

    [LocalMerchantSqlFact]
    public async Task UnassignedDriverCannotCreateDeliveryOrFinancialRecords()
    {
        await using var database = await TestDatabase.CreateAsync();
        var ids = await PendingDelivery(database);
        await using var db = database.Context();
        var other = new Driver { Name = "Other driver" }; db.Drivers.Add(other); await db.SaveChangesAsync();
        Assert.False((await new ShipmentService(db).SubmitPodAsync(ids.Shipment, other.DriverId, "simulation", null)).IsSuccess);
        Assert.Empty(await db.DriverPayments.ToListAsync()); Assert.Empty(await db.ShipmentAttempts.ToListAsync());
    }

    [LocalMerchantSqlFact]
    public async Task SqlCredentialUpdatesRevokeSessionsWithoutExposingCredentialColumnsToTheValidator()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var db = database.Context();
        var merchant = await db.Merchants.SingleAsync();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, merchant.MerchantId.ToString()), new Claim("MerchantId", merchant.MerchantId.ToString()),
            new Claim(ClaimTypes.Role, "Merchant"), new Claim(AccountSessionValidator.TypeClaim, "Merchant"),
            new Claim(AccountSessionValidator.StampClaim, merchant.SecurityStamp.ToString())
        }, "test"));
        var validator = new AccountSessionValidator(db);
        Assert.True(await validator.ValidateAsync(principal));
        merchant.Password = PasswordHashing.HashNewPassword("Sql-changed-password-123");
        await db.SaveChangesAsync();
        Assert.False(await validator.ValidateAsync(principal));
    }

    [LocalMerchantSqlFact]
    public async Task SessionMigrationPreservesAccountsAndCreatesDistinctNonemptyStampsForLegacyRows()
    {
        await using var database = await TestDatabase.CreateAsync();
        await PendingDelivery(database);
        await using var db = database.Context();
        db.Merchants.Add(new Merchant { ShopName = "Another", Email = "other@example.invalid", Password = "not-a-login" });
        await db.SaveChangesAsync();
        await PreviousSchema(db);
        await PrepareHistory(db);
        await db.GetService<IMigrator>().MigrateAsync();
        db.ChangeTracker.Clear();
        var stamps = await db.Merchants.Select(m => m.SecurityStamp).ToListAsync();
        stamps.AddRange(await db.Employees.Select(e => e.SecurityStamp).ToListAsync());
        Assert.Equal(3, stamps.Count); Assert.DoesNotContain(Guid.Empty, stamps);
        Assert.Equal(stamps.Count, stamps.Distinct().Count());
        Assert.Equal(8, (await db.Shipments.SingleAsync()).RowVersion.Length);
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [LocalMerchantSqlFact]
    public async Task MigrationStopsForDuplicateHistoricalPayoutsWithoutDeletingThemOrChangingSchema()
    {
        await using var database = await TestDatabase.CreateAsync();
        var ids = await PendingDelivery(database);
        await using var db = database.Context();
        await PreviousSchema(db); await PrepareHistory(db);
        for (var i = 0; i < 2; i++)
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [Driver_Payment] ([DriverId],[ShipmentId],[TotalPayout],[PaymentStatus],[CreatedAt]) VALUES ({ids.Driver},{ids.Shipment},15,N'Pending',SYSUTCDATETIME())");
        var exception = await Assert.ThrowsAsync<SqlException>(() => db.GetService<IMigrator>().MigrateAsync());
        Assert.Equal(51001, exception.Number);
        Assert.Contains("operator review", exception.Message);
        Assert.Equal(2, await db.DriverPayments.CountAsync());
        Assert.DoesNotContain((await db.Database.GetAppliedMigrationsAsync()), m => m.EndsWith("ProtectSessionsAndDeliveryPayouts"));
    }

    private static async Task<(int Shipment, int Driver)> PendingDelivery(TestDatabase database)
    {
        await using var db = database.Context();
        var driver = new Driver { Name = "Test driver", Employee = new Employee { Name = "Driver", Email = "driver@example.invalid", Password = "not-a-login" } };
        db.Drivers.Add(driver); await db.SaveChangesAsync();
        var created = await new MerchantOperationsService(db).CreateShipmentAsync(MerchantWriteData.Input(), MerchantWriteData.User());
        Assert.Equal(200, created.StatusCode);
        var shipment = await db.Shipments.SingleAsync(); shipment.DriverId = driver.DriverId; await db.SaveChangesAsync();
        return (shipment.ShipmentId, driver.DriverId);
    }

    private static async Task PrepareHistory(DmsDbContext db)
    {
        var history = db.GetService<IHistoryRepository>();
        await db.Database.ExecuteSqlRawAsync(history.GetCreateScript());
        foreach (var applied in db.Database.GetMigrations().Where(m => !m.EndsWith("ProtectSessionsAndDeliveryPayouts")))
            await db.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(applied, "10.0.9")));
    }

    private static Task PreviousSchema(DmsDbContext db) => db.Database.ExecuteSqlRawAsync("""
        DROP INDEX [IX_Driver_Payment_ShipmentId] ON [Driver_Payment];
        CREATE INDEX [IX_Driver_Payment_ShipmentId] ON [Driver_Payment] ([ShipmentId]);
        ALTER TABLE [Shipment] DROP COLUMN [DeliveryConfirmedAt], [RowVersion];
        DECLARE @employeeDefault sysname = (SELECT dc.name FROM sys.default_constraints dc JOIN sys.columns c
            ON dc.parent_object_id=c.object_id AND dc.parent_column_id=c.column_id
            WHERE dc.parent_object_id=OBJECT_ID(N'Employee') AND c.name=N'SecurityStamp');
        IF @employeeDefault IS NOT NULL
        BEGIN
            DECLARE @dropEmployee nvarchar(max) = N'ALTER TABLE [Employee] DROP CONSTRAINT '+QUOTENAME(@employeeDefault);
            EXEC(@dropEmployee);
        END;
        ALTER TABLE [Employee] DROP COLUMN [SecurityStamp];
        DECLARE @merchantDefault sysname = (SELECT dc.name FROM sys.default_constraints dc JOIN sys.columns c
            ON dc.parent_object_id=c.object_id AND dc.parent_column_id=c.column_id
            WHERE dc.parent_object_id=OBJECT_ID(N'Merchant') AND c.name=N'SecurityStamp');
        IF @merchantDefault IS NOT NULL
        BEGIN
            DECLARE @dropMerchant nvarchar(max) = N'ALTER TABLE [Merchant] DROP CONSTRAINT '+QUOTENAME(@merchantDefault);
            EXEC(@dropMerchant);
        END;
        ALTER TABLE [Merchant] DROP COLUMN [SecurityStamp];
        """);

    private sealed class TwoDeliveryWriters : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int writers;
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref writers) == 2) ready.TrySetResult();
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken); return result;
        }
    }
}
