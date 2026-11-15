using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.RegularExpressions;
using FastDrop.MVC.Models;
using FastDrop.MVC.Security;
using FastDrop.MVC.Services;
using FastDrop.MVC.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace FastDrop.MVC.Tests;

[Collection("Shipment read HTTP host")]
public class MerchantWriteSecurityTests : IClassFixture<MerchantWriteFixture>
{
    private readonly MerchantWriteFixture fixture;
    public MerchantWriteSecurityTests(MerchantWriteFixture fixture) { this.fixture = fixture; fixture.Reset(); }

    [Theory]
    [InlineData("EditShipment", 102)]
    [InlineData("EditProduct", 2)]
    [InlineData("EditShipment", 999)]
    [InlineData("EditProduct", 999)]
    public async Task ReadsAndForgedPostsCannotEditAnotherMerchantsResource(string action, int id)
    {
        using var client = fixture.Client();
        using var read = await client.GetAsync($"/MerchantPortal/{action}/{id}");
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        var form = action == "EditShipment" ? ShipmentEdit() : ProductEdit();
        form["MerchantId"] = "1"; form["ShipmentId"] = id.ToString(); form["ProductId"] = id.ToString();
        using var response = await fixture.Post(client, $"/MerchantPortal/{action}/{id}", form);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await fixture.Database(db =>
        {
            Assert.Equal(2m, db.Shipments.Single(s => s.ShipmentId == 102).WeightKg);
            Assert.Equal(300m, db.Products.Single(p => p.ProductId == 2).Price);
        });
    }

    [Fact]
    public async Task OwnShipmentEditIgnoresOwnershipAssignmentAndMoneyAndClonesSharedAddress()
    {
        using var client = fixture.Client();
        var form = ShipmentEdit();
        form["MerchantId"] = "2"; form["DriverId"] = "21"; form["RouteId"] = "99";
        form["WarehouseId"] = "99"; form["Address.AddressId"] = "1"; form["CodAmount"] = "0";
        form["ShipmentStatuses[0].Status"] = "Delivered";
        form["ShipmentProducts[0].ProductId"] = "2";
        using var response = await fixture.Post(client, "/MerchantPortal/EditShipment/101", form);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await fixture.Database(db =>
        {
            var own = db.Shipments.Include(s => s.Address).Single(s => s.ShipmentId == 101);
            Assert.Equal(1, own.MerchantId); Assert.Null(own.DriverId); Assert.Null(own.RouteId); Assert.Null(own.WarehouseId);
            Assert.Equal(5m, own.WeightKg); Assert.Equal(220m, own.CodAmount);
            Assert.Equal("New street", own.Address!.Street); Assert.NotEqual(1, own.AddressId);
            Assert.Equal("Shared street", db.Addresses.Single(a => a.AddressId == 1).Street);
            Assert.Equal(1, db.Shipments.Single(s => s.ShipmentId == 102).AddressId);
            Assert.Empty(db.ShipmentStatuses); Assert.Single(db.ShipmentProducts);
        });
    }

    [Fact]
    public async Task OwnProductEditWorksWithoutPostedOwnerAndCannotTransferOwnership()
    {
        using var client = fixture.Client();
        var page = await client.GetStringAsync("/MerchantPortal/EditProduct/1");
        Assert.DoesNotContain("name=\"MerchantId\"", page);
        Assert.Contains("name=\"OriginalStockQuantity\"", page);
        var form = ProductEdit(); form["MerchantId"] = "2"; form["ProductId"] = "2";
        using var response = await fixture.Post(client, "/MerchantPortal/EditProduct/1", form);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await fixture.Database(db =>
        {
            var product = db.Products.Single(p => p.ProductId == 1);
            Assert.Equal(1, product.MerchantId); Assert.Equal("Updated product", product.Name); Assert.Equal(125m, product.Price);
            Assert.Equal(300m, db.Products.Single(p => p.ProductId == 2).Price);
            Assert.Equal(100m, db.ShipmentProducts.Single().UnitPrice);
        });
    }

    [Fact]
    public async Task ShipmentCreationUsesStoredPriceReservesStockAndIgnoresAnEntireForgedGraph()
    {
        using var client = fixture.Client();
        var form = Create();
        form["SelectedProducts[0].Price"] = "0";
        form["SelectedProducts[0].StockQuantity"] = "999999";
        form["SelectedProducts[0].ProductName"] = "<script>forged</script>";
        foreach (var field in new[] { "MerchantId", "DriverId", "RouteId", "WarehouseId", "AddressId", "ShipmentId" }) form["Shipment." + field] = "999";
        form["Shipment.CodAmount"] = "-100"; form["Shipment.CreationDate"] = "2000-01-01";
        form["Shipment.Address.AddressId"] = "1"; form["Shipment.Address.Zone.ZoneId"] = "999";
        form["Shipment.Merchant.ShopName"] = "Forged shop";
        form["Shipment.ShipmentStatuses[0].Status"] = "Delivered";
        form["Shipment.ShipmentAttempts[0].Status"] = "Success";
        form["Shipment.Invoices[0].AmountDue"] = "999";
        form["Shipment.Feedbacks[0].Rating"] = "5";
        form["Shipment.ShipmentProducts[0].ProductId"] = "2";
        using var response = await fixture.Post(client, "/MerchantPortal/AddShipment", form);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await fixture.Database(db =>
        {
            var shipment = db.Shipments.Include(s => s.Address).Include(s => s.ShipmentProducts)
                .Include(s => s.ShipmentStatuses).Single(s => s.ShipmentId != 101 && s.ShipmentId != 102);
            Assert.Equal(1, shipment.MerchantId); Assert.Null(shipment.DriverId); Assert.Null(shipment.RouteId); Assert.Null(shipment.WarehouseId);
            Assert.Equal(220m, shipment.CodAmount); Assert.True(shipment.CreationDate > DateTime.UtcNow.AddMinutes(-1));
            Assert.NotEqual(1, shipment.AddressId); Assert.Equal(1, shipment.Address!.ZoneId);
            var line = Assert.Single(shipment.ShipmentProducts);
            Assert.Equal(1, line.ProductId); Assert.Equal(2, line.Quantity); Assert.Equal(100m, line.UnitPrice);
            Assert.Equal(3, db.Products.Single(p => p.ProductId == 1).StockQuantity);
            Assert.Equal("Pending", Assert.Single(shipment.ShipmentStatuses).Status);
            Assert.Empty(db.ShipmentAttempts); Assert.Empty(db.Invoices); Assert.Empty(db.Feedbacks);
            Assert.Equal(2, db.Merchants.Count()); Assert.Single(db.Zones);
        });
    }

    [Theory]
    [InlineData("foreign")]
    [InlineData("missing")]
    [InlineData("zero")]
    [InlineData("negative")]
    [InlineData("too-many")]
    [InlineData("duplicate")]
    [InlineData("none-selected")]
    [InlineData("invalid-zone")]
    [InlineData("missing-zone")]
    [InlineData("inactive-zone")]
    [InlineData("bad-address")]
    [InlineData("bad-weight")]
    [InlineData("invalid-price")]
    [InlineData("overflow-total")]
    public async Task InvalidOrdersDoNotPersistAnyAddressShipmentOrStockChange(string variant)
    {
        using var client = fixture.Client();
        var form = Create();
        if (variant == "foreign") form["SelectedProducts[0].ProductId"] = "2";
        if (variant == "missing") form["SelectedProducts[0].ProductId"] = "999";
        if (variant == "zero") form["SelectedProducts[0].Quantity"] = "0";
        if (variant == "negative") form["SelectedProducts[0].Quantity"] = "-1";
        if (variant == "too-many") form["SelectedProducts[0].Quantity"] = "6";
        if (variant == "duplicate") { form["SelectedProducts[1].ProductId"] = "1"; form["SelectedProducts[1].IsSelected"] = "true"; form["SelectedProducts[1].Quantity"] = "1"; }
        if (variant == "none-selected") form["SelectedProducts[0].IsSelected"] = "false";
        if (variant == "invalid-zone") form["Shipment.Address.ZoneId"] = "999";
        if (variant == "missing-zone") form.Remove("Shipment.Address.ZoneId");
        if (variant == "bad-address") form["Shipment.Address.Street"] = " ";
        if (variant == "bad-weight") form["Shipment.WeightKg"] = "-5";
        if (variant == "inactive-zone") await fixture.Mutate(db => db.Zones.Single().ActiveStatus = false);
        if (variant == "invalid-price") await fixture.Mutate(db => db.Products.Single(p => p.ProductId == 1).Price = -1);
        if (variant == "overflow-total") await fixture.Mutate(db => db.Products.Single(p => p.ProductId == 1).Price = 99999999.99m);
        using var response = await fixture.Post(client, "/MerchantPortal/AddShipment", form);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var page = await response.Content.ReadAsStringAsync();
        Assert.Contains("Available product", page); Assert.DoesNotContain("Foreign product", page);
        await fixture.Database(db =>
        {
            Assert.Equal(2, db.Shipments.Count()); Assert.Single(db.Addresses);
            Assert.Equal(5, db.Products.Single(p => p.ProductId == 1).StockQuantity);
            Assert.Single(db.ShipmentProducts); Assert.Empty(db.ShipmentStatuses);
        });
    }

    [Fact]
    public async Task LegacyMerchantCreateCannotBypassValidatedCreation()
    {
        using var client = fixture.Client();
        using var page = await client.GetAsync("/Shipments/Create");
        Assert.Equal("/MerchantPortal/AddShipment", page.Headers.Location?.OriginalString);
        using var response = await fixture.Post(client, "/Shipments/Create", new() { ["MerchantId"] = "1", ["CodAmount"] = "0", ["RouteId"] = "999" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await fixture.Database(db => Assert.Equal(2, db.Shipments.Count()));
    }

    [Theory]
    [InlineData("/MerchantPortal/AddShipment")]
    [InlineData("/MerchantPortal/EditShipment/101")]
    [InlineData("/MerchantPortal/EditProduct/1")]
    public async Task AllWritesRequireAntiforgery(string path)
    {
        using var client = fixture.Client();
        using var response = await client.PostAsync(path, new FormUrlEncodedContent(Create()));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AStaleProductFormCannotUndoAStockReservation()
    {
        using var client = fixture.Client();
        using var create = await fixture.Post(client, "/MerchantPortal/AddShipment", Create());
        Assert.Equal(HttpStatusCode.Redirect, create.StatusCode);
        using var edit = await fixture.Post(client, "/MerchantPortal/EditProduct/1", ProductEdit());
        Assert.Equal(HttpStatusCode.Conflict, edit.StatusCode);
        await fixture.Database(db => Assert.Equal(3, db.Products.Single(p => p.ProductId == 1).StockQuantity));
    }

    [Fact]
    public async Task PendingShipmentDeletionReleasesOnlyItsNewReservationsOnce()
    {
        using var client = fixture.Client();
        using var create = await fixture.Post(client, "/MerchantPortal/AddShipment", Create());
        int id = 0;
        await fixture.Database(db => id = db.Shipments.Single(s => s.ShipmentId != 101 && s.ShipmentId != 102).ShipmentId);
        using var delete = await fixture.Post(client, $"/MerchantPortal/DeleteShipment/{id}", new());
        using var retry = await fixture.Post(client, $"/MerchantPortal/DeleteShipment/{id}", new());
        Assert.Equal(HttpStatusCode.Redirect, delete.StatusCode); Assert.Equal(HttpStatusCode.Redirect, retry.StatusCode);
        await fixture.Database(db => Assert.Equal(5, db.Products.Single(p => p.ProductId == 1).StockQuantity));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("mismatch")]
    [InlineData("negative")]
    public async Task AmbiguousMerchantIdentityCannotReachAnyWrite(string variant)
    {
        var claims = MerchantWriteData.Claims().ToList();
        if (variant == "missing") claims.RemoveAll(c => c.Type == "MerchantId");
        if (variant == "duplicate") claims.Add(new("MerchantId", "2"));
        if (variant is "mismatch" or "negative") { claims.RemoveAll(c => c.Type == "MerchantId"); claims.Add(new("MerchantId", variant == "mismatch" ? "2" : "-1")); }
        using var scope = fixture.Application.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IMerchantOperationsService>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        Assert.Equal(404, (await service.CreateShipmentAsync(MerchantWriteData.Input(), user)).StatusCode);
        Assert.Equal(404, (await service.EditShipmentAsync(101, new(), user)).StatusCode);
        Assert.Equal(404, (await service.EditProductAsync(1, new(), user)).StatusCode);
    }

    public static Dictionary<string, string> Create() => new()
    {
        ["Shipment.Address.Street"] = "New street", ["Shipment.Address.District"] = "District", ["Shipment.Address.ZoneId"] = "1",
        ["Shipment.WeightKg"] = "5", ["SelectedProducts[0].ProductId"] = "1", ["SelectedProducts[0].IsSelected"] = "true", ["SelectedProducts[0].Quantity"] = "2"
    };
    private static Dictionary<string, string> ShipmentEdit() => new()
    {
        ["Address.Street"] = "New street", ["Address.District"] = "District", ["Address.ZoneId"] = "1", ["WeightKg"] = "5"
    };
    private static Dictionary<string, string> ProductEdit() => new()
    {
        ["Name"] = "Updated product", ["Price"] = "125", ["StockQuantity"] = "5", ["OriginalStockQuantity"] = "5"
    };
}

public sealed class MerchantWriteFixture : IDisposable
{
    private readonly ManagementApplicationFactory root = new();
    public WebApplicationFactory<Program> Application { get; }
    public MerchantWriteFixture()
    {
        var name = Guid.NewGuid().ToString();
        Application = root.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<DmsDbContext>();
            services.AddScoped(_ => new DmsDbContext(new DbContextOptionsBuilder<DmsDbContext>().UseInMemoryDatabase(name).Options));
        }));
    }
    public void Reset()
    {
        using var scope = Application.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DmsDbContext>();
        db.Database.EnsureDeleted(); MerchantWriteData.Seed(db).GetAwaiter().GetResult();
    }
    public HttpClient Client()
    {
        var client = Application.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var settings = Application.Services.GetRequiredService<JwtSettings>();
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, MerchantWriteData.Claims(), expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(settings.SigningKey, SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }
    public async Task<HttpResponseMessage> Post(HttpClient client, string path, Dictionary<string, string> fields)
    {
        var page = await client.GetStringAsync("/MerchantPortal/AddShipment");
        var match = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success);
        fields["__RequestVerificationToken"] = WebUtility.HtmlDecode(match.Groups[1].Value);
        return await client.PostAsync(path, new FormUrlEncodedContent(fields));
    }
    public Task Database(Action<DmsDbContext> assert)
    {
        using var scope = Application.Services.CreateScope(); assert(scope.ServiceProvider.GetRequiredService<DmsDbContext>()); return Task.CompletedTask;
    }
    public async Task Mutate(Action<DmsDbContext> edit)
    {
        using var scope = Application.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DmsDbContext>(); edit(db); await db.SaveChangesAsync();
    }
    public void Dispose() { Application.Dispose(); root.Dispose(); }
}

internal static class MerchantWriteData
{
    public static IEnumerable<Claim> Claims() => new[] { new Claim(ClaimTypes.Role, "Merchant"),
        new Claim(ClaimTypes.NameIdentifier, "1"), new Claim("MerchantId", "1"), new Claim(ClaimTypes.Name, "Merchant One") };
    public static ClaimsPrincipal User() => new(new ClaimsIdentity(Claims(), "test"));
    public static AddShipmentViewModel Input(int quantity = 2) => new()
    {
        Shipment = new() { Address = new() { Street = "New street", District = "District", ZoneId = 1 }, WeightKg = 5 },
        SelectedProducts = new() { new() { ProductId = 1, Quantity = quantity, IsSelected = true } }
    };
    public static async Task Seed(DmsDbContext db)
    {
        db.Merchants.AddRange(new Merchant { MerchantId = 1, ShopName = "Merchant One", Email = "one@example.invalid", Password = "not-a-login" },
            new Merchant { MerchantId = 2, ShopName = "Merchant Two", Email = "two@example.invalid", Password = "not-a-login" });
        db.Zones.Add(new Zone { ZoneId = 1, Name = "Test zone", CodAmount = 20, DeliveryFee = 30 });
        db.Addresses.Add(new Address { AddressId = 1, Street = "Shared street", District = "District", ZoneId = 1 });
        db.Products.AddRange(new Product { ProductId = 1, MerchantId = 1, Name = "Available product", Price = 100, StockQuantity = 5 },
            new Product { ProductId = 2, MerchantId = 2, Name = "Foreign product", Price = 300, StockQuantity = 5 });
        db.Shipments.AddRange(new Shipment { ShipmentId = 101, MerchantId = 1, AddressId = 1, WeightKg = 2, CodAmount = 220 },
            new Shipment { ShipmentId = 102, MerchantId = 2, AddressId = 1, WeightKg = 2, CodAmount = 220 });
        db.ShipmentProducts.Add(new ShipmentProduct { ShipmentId = 101, ProductId = 1, Quantity = 2, UnitPrice = 100 });
        await db.SaveChangesAsync();
    }
}
