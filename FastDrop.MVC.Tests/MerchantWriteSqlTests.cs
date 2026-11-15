using System.Security.Claims;
using FastDrop.MVC.Models;
using FastDrop.MVC.Services;
using FastDrop.MVC.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace FastDrop.MVC.Tests;

// These tests create and remove only uniquely named databases on a dedicated LocalDB instance.
// Ordinary runs skip them; see README for the explicit opt-in and instance setup.
public sealed class LocalMerchantSqlFactAttribute : FactAttribute
{
    public LocalMerchantSqlFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("FASTDROP_RUN_SQL_TESTS") != "1")
            Skip = "Set FASTDROP_RUN_SQL_TESTS=1 with the isolated FastDropSecurityAudit LocalDB instance running.";
    }
}

public class MerchantWriteSqlTests
{
    [LocalMerchantSqlFact]
    public async Task ConcurrentReservationsCannotOversellOrLeavePartialOrders()
    {
        await using var database = await TestDatabase.CreateAsync();
        var gate = new TwoWriterGate();
        await using var first = database.Context(gate);
        await using var second = database.Context(gate);
        var results = await Task.WhenAll(
            new MerchantOperationsService(first).CreateShipmentAsync(MerchantWriteData.Input(3), MerchantWriteData.User()),
            new MerchantOperationsService(second).CreateShipmentAsync(MerchantWriteData.Input(3), MerchantWriteData.User()));
        Assert.Single(results, r => r.StatusCode == 200);
        Assert.Single(results, r => r.StatusCode == 409);
        await using var check = database.Context();
        Assert.Equal(2, (await check.Products.FindAsync(1))!.StockQuantity);
        Assert.Equal(1, await check.Shipments.CountAsync());
        Assert.Equal(1, await check.Addresses.CountAsync());
        Assert.Equal(1, await check.ShipmentProducts.CountAsync());
        Assert.Equal(1, await check.ShipmentStatuses.CountAsync());
    }

    [LocalMerchantSqlFact]
    public async Task AStaleSecondProductRollsBackTheFirstProductAddressAndOrder()
    {
        await using var database = await TestDatabase.CreateAsync();
        var change = new BeforeSave(async () =>
        {
            await using var other = database.Context();
            await other.Products.Where(p => p.ProductId == 2).ExecuteUpdateAsync(set => set.SetProperty(p => p.StockQuantity, 4));
        });
        await using var db = database.Context(change);
        var input = MerchantWriteData.Input();
        input.SelectedProducts.Add(new SelectedProduct { ProductId = 2, Quantity = 2, IsSelected = true });
        var result = await new MerchantOperationsService(db).CreateShipmentAsync(input, MerchantWriteData.User());
        Assert.Equal(409, result.StatusCode);
        await using var check = database.Context();
        Assert.Equal(5, (await check.Products.FindAsync(1))!.StockQuantity);
        Assert.Equal(4, (await check.Products.FindAsync(2))!.StockQuantity);
        Assert.Empty(await check.Addresses.ToListAsync());
        Assert.Empty(await check.Shipments.ToListAsync());
        Assert.Empty(await check.ShipmentProducts.ToListAsync());
        Assert.Empty(await check.ShipmentStatuses.ToListAsync());
    }

    [LocalMerchantSqlFact]
    public async Task PriceSnapshotsSurviveCatalogEditsAndAreUsedForDestinationChanges()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var db = database.Context();
        var service = new MerchantOperationsService(db);
        var created = await service.CreateShipmentAsync(MerchantWriteData.Input(), MerchantWriteData.User());
        Assert.Equal(200, created.StatusCode);
        var edit = await service.GetProductEditAsync(1, MerchantWriteData.User());
        edit!.Price = 999;
        Assert.Equal(200, (await service.EditProductAsync(1, edit, MerchantWriteData.User())).StatusCode);
        var zone = new Zone { Name = "Another zone", CodAmount = 50 };
        db.Zones.Add(zone); await db.SaveChangesAsync();
        var result = await service.EditShipmentAsync(created.ShipmentId!.Value, new MerchantShipmentEditViewModel
        {
            Address = new MerchantAddressInput { Street = "Another street", District = "District", ZoneId = zone.ZoneId }, WeightKg = 1
        }, MerchantWriteData.User());
        Assert.Equal(200, result.StatusCode);
        db.ChangeTracker.Clear();
        Assert.Equal(250m, (await db.Shipments.SingleAsync()).CodAmount);
        Assert.Equal(100m, (await db.ShipmentProducts.SingleAsync()).UnitPrice);
    }

    [LocalMerchantSqlFact]
    public async Task MigrationAddsNullablePriceWithoutInventingHistoricalValues()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var db = database.Context();
        var legacy = new Shipment { MerchantId = 1, Address = new Address { ZoneId = 1, Street = "Legacy", District = "District" } };
        legacy.ShipmentProducts.Add(new ShipmentProduct { ProductId = 1, Quantity = 1 });
        db.Shipments.Add(legacy); await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE [Shipment_Product] DROP COLUMN [UnitPrice]");
        var history = db.GetService<IHistoryRepository>();
        await db.Database.ExecuteSqlRawAsync(history.GetCreateScript());
        var target = db.Database.GetMigrations().Last();
        Assert.EndsWith("CaptureShipmentUnitPrices", target);
        foreach (var applied in db.Database.GetMigrations().Where(m => m != target))
            await db.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(applied, "10.0.9")));
        await db.GetService<IMigrator>().MigrateAsync(target);
        db.ChangeTracker.Clear();
        Assert.Null((await db.ShipmentProducts.SingleAsync()).UnitPrice);
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [LocalMerchantSqlFact]
    public async Task StaffUpdatesUseStoredConcurrencyValuesWhenChangingStockPriceAndOwner()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var db = database.Context();
        var other = new Merchant { ShopName = "Another merchant", Email = "another@example.invalid", Password = "not-a-login" };
        var warehouse = new Warehouse { WarehouseName = "Test warehouse" };
        db.Merchants.Add(other); db.Warehouses.Add(warehouse); await db.SaveChangesAsync();
        await new ProductService(db).UpdateProductAsync(new Product { ProductId = 1, MerchantId = 1,
            Name = "Staff edit", Price = 110 }, new Dictionary<int, int> { [warehouse.WarehouseId] = 8 });
        var created = await new MerchantOperationsService(db).CreateShipmentAsync(MerchantWriteData.Input(), MerchantWriteData.User());
        Assert.Equal(200, created.StatusCode);
        var shipment = await db.Shipments.AsNoTracking().SingleAsync();
        await new ShipmentService(db).UpdateShipmentAsync(new Shipment
        {
            ShipmentId = shipment.ShipmentId, MerchantId = other.MerchantId,
            AddressId = shipment.AddressId, CodAmount = shipment.CodAmount, CreationDate = shipment.CreationDate
        });
        db.ChangeTracker.Clear();
        Assert.Equal(6, (await db.Products.FindAsync(1))!.StockQuantity);
        Assert.Equal(110m, (await db.Products.FindAsync(1))!.Price);
        Assert.Equal(other.MerchantId, (await db.Shipments.SingleAsync()).MerchantId);
    }

    private sealed class TwoWriterGate : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int writers;
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref writers) == 2) ready.TrySetResult();
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
            return result;
        }
    }
    private sealed class BeforeSave(Func<Task> callback) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await callback(); return result;
        }
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string name = "FastDrop_SecurityTests_" + Guid.NewGuid().ToString("N");
        private string Connection => new SqlConnectionStringBuilder
        {
            DataSource = @"(localdb)\FastDropSecurityAudit", InitialCatalog = name,
            IntegratedSecurity = true, TrustServerCertificate = true, Encrypt = false, ConnectTimeout = 15
        }.ConnectionString;
        public DmsDbContext Context(params IInterceptor[] interceptors) => new(new DbContextOptionsBuilder<DmsDbContext>()
            .UseSqlServer(Connection).AddInterceptors(interceptors).Options);
        public static async Task<TestDatabase> CreateAsync()
        {
            var database = new TestDatabase();
            try
            {
                await using var db = database.Context();
                await db.Database.EnsureCreatedAsync();
                var merchant = new Merchant { ShopName = "SQL test merchant", Email = "sql@example.invalid", Password = "not-a-login" };
                db.Merchants.Add(merchant); db.Zones.Add(new Zone { Name = "Test zone", CodAmount = 20 });
                await db.SaveChangesAsync();
                db.Products.Add(new Product { MerchantId = merchant.MerchantId, Name = "First product", Price = 100, StockQuantity = 5 });
                await db.SaveChangesAsync();
                db.Products.Add(new Product { MerchantId = merchant.MerchantId, Name = "Second product", Price = 100, StockQuantity = 5 });
                await db.SaveChangesAsync();
                Assert.Equal(1, merchant.MerchantId);
                return database;
            }
            catch { await database.DisposeAsync(); throw; }
        }
        public async ValueTask DisposeAsync()
        {
            if (!name.StartsWith("FastDrop_SecurityTests_", StringComparison.Ordinal) || name.Length != 55)
                throw new InvalidOperationException("Unexpected test database name.");
            await using var db = Context(); await db.Database.EnsureDeletedAsync();
        }
    }
}
