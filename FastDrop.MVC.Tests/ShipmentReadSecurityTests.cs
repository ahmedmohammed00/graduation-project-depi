using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using FastDrop.MVC.Models;
using FastDrop.MVC.Security;
using FastDrop.MVC.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Route = FastDrop.MVC.Models.Route;

namespace FastDrop.MVC.Tests;

[Collection("Shipment read HTTP host")]
public class ShipmentReadSecurityTests(ShipmentReadFixture fixture) : IClassFixture<ShipmentReadFixture>
{
    [Theory]
    [InlineData("Merchant", "1", 101, HttpStatusCode.OK)]
    [InlineData("Merchant", "1", 102, HttpStatusCode.NotFound)]
    [InlineData("Merchant", "1", 103, HttpStatusCode.OK)]
    [InlineData("Merchant", "2", 101, HttpStatusCode.NotFound)]
    [InlineData("Driver", "1", 101, HttpStatusCode.OK)]
    [InlineData("Driver", "1", 102, HttpStatusCode.NotFound)]
    [InlineData("Driver", "1", 103, HttpStatusCode.OK)]
    [InlineData("Driver", "1", 104, HttpStatusCode.NotFound)]
    [InlineData("Driver", "1", 105, HttpStatusCode.NotFound)]
    [InlineData("Driver", "1", 106, HttpStatusCode.NotFound)]
    [InlineData("Driver", "2", 104, HttpStatusCode.OK)]
    [InlineData("Driver", "2", 106, HttpStatusCode.OK)]
    [InlineData("SuperAdmin", null, 102, HttpStatusCode.OK)]
    [InlineData("Dispatcher", null, 106, HttpStatusCode.OK)]
    [InlineData("Employee", null, 101, HttpStatusCode.Forbidden)]
    [InlineData("Unknown", "1", 101, HttpStatusCode.Forbidden)]
    public async Task GenericDetailsEnforcesOwnershipAndAssignment(string role, string? ownerId,
        int shipmentId, HttpStatusCode expected)
    {
        using var client = fixture.Client(role, ownerId);
        using var response = await client.GetAsync($"/Shipments/Details/{shipmentId}");
        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData("Merchant", null)]
    [InlineData("Merchant", "not-an-id")]
    [InlineData("Merchant", "0")]
    [InlineData("Merchant", "-1")]
    [InlineData("Driver", null)]
    [InlineData("Driver", "not-an-id")]
    [InlineData("Driver", "0")]
    [InlineData("Driver", "-1")]
    public async Task MissingOrInvalidOwnershipClaimsDenyAccess(string role, string? ownerId)
    {
        using var client = fixture.Client(role, ownerId);
        using var response = await client.GetAsync("/Shipments/Details/101");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("Merchant", "MerchantId")]
    [InlineData("Driver", "DriverId")]
    public async Task DuplicateOwnershipClaimsAreRejected(string role, string claimType)
    {
        using var client = fixture.Client(role, "1", new Claim(claimType, "2"));
        using var response = await client.GetAsync("/Shipments/Details/101");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/MerchantPortal/ShipmentDetails/101", "Merchant", "1", HttpStatusCode.OK)]
    [InlineData("/MerchantPortal/ShipmentDetails/102", "Merchant", "1", HttpStatusCode.NotFound)]
    [InlineData("/DriverPortal/UpdateStatus/101", "Driver", "1", HttpStatusCode.OK)]
    [InlineData("/DriverPortal/UpdateStatus/103", "Driver", "1", HttpStatusCode.OK)]
    [InlineData("/DriverPortal/UpdateStatus/102", "Driver", "1", HttpStatusCode.NotFound)]
    [InlineData("/DriverPortal/UpdateStatus/105", "Driver", "1", HttpStatusCode.NotFound)]
    [InlineData("/AdminPortal/ShipmentDetails/102", "SuperAdmin", null, HttpStatusCode.OK)]
    public async Task PortalReadRoutesUseTheSharedScope(string path, string role, string? ownerId, HttpStatusCode expected)
    {
        using var client = fixture.Client(role, ownerId);
        using var response = await client.GetAsync(path);
        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(999)]
    public async Task MissingAndInvalidShipmentIdsDoNotExposeDetails(int shipmentId)
    {
        using var client = fixture.Client("Merchant", "1");
        using var response = await client.GetAsync($"/Shipments/Details/{shipmentId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousDetailsRequiresLogin()
    {
        using var client = fixture.Client();
        using var response = await client.GetAsync("/Shipments/Details/101");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task ServiceDoesNotTrustRolesOnAnUnauthenticatedPrincipal()
    {
        using var scope = fixture.Application.Services.CreateScope();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "SuperAdmin") }));
        Assert.Null(await scope.ServiceProvider.GetRequiredService<IShipmentService>()
            .GetVisibleShipmentByIdAsync(101, user));
    }
}

[CollectionDefinition("Shipment read HTTP host", DisableParallelization = true)]
public sealed class ShipmentReadTestCollection { }

public sealed class ShipmentReadFixture : IAsyncLifetime
{
    private readonly ManagementApplicationFactory _parent = new();
    public WebApplicationFactory<Program> Application { get; }

    public ShipmentReadFixture()
    {
        var name = Guid.NewGuid().ToString();
        Application = _parent.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<DmsDbContext>();
            services.AddScoped(_ => new DmsDbContext(new DbContextOptionsBuilder<DmsDbContext>()
                .UseInMemoryDatabase(name).Options));
        }));
    }

    public async Task InitializeAsync()
    {
        using var scope = Application.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DmsDbContext>();
        var hash = BCrypt.Net.BCrypt.HashPassword("Fixture-only-password");
        db.Merchants.AddRange(
            new Merchant { MerchantId = 1, ShopName = "One", Email = "one@example.invalid", Password = hash },
            new Merchant { MerchantId = 2, ShopName = "Two", Email = "two@example.invalid", Password = hash });
        db.Drivers.AddRange(new Driver { DriverId = 1, Name = "One" }, new Driver { DriverId = 2, Name = "Two" });
        db.Routes.AddRange(new Route { RouteId = 10, RouteName = "One" }, new Route { RouteId = 20, RouteName = "Two" });
        db.DriverRoutes.AddRange(new DriverRoute { DriverId = 1, RouteId = 10 }, new DriverRoute { DriverId = 2, RouteId = 20 });
        db.Addresses.Add(new Address { AddressId = 1, Street = "Fixture Street" });
        db.Warehouses.Add(new Warehouse { WarehouseId = 1, WarehouseName = "Fixture Warehouse" });
        db.Shipments.AddRange(
            Shipment(101, 1, 1, null), Shipment(102, 2, 2, null), Shipment(103, 1, null, 10),
            Shipment(104, 2, null, 20), Shipment(105, 1, null, null), Shipment(106, 2, 1, 20));
        await db.SaveChangesAsync();
    }

    public HttpClient Client(string? role = null, string? ownerId = null, params Claim[] additionalClaims)
    {
        var client = Application.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
        });
        if (role == null) return client;
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role), new(ClaimTypes.NameIdentifier, role == "Merchant" ? ownerId ?? "1" : "fixture-user"), new(ClaimTypes.Name, "Fixture User")
        };
        if (ownerId != null) claims.Add(new Claim(role == "Merchant" ? "MerchantId" : "DriverId", ownerId));
        claims.AddRange(additionalClaims);
        var settings = Application.Services.GetRequiredService<JwtSettings>();
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(settings.SigningKey, SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }

    public async Task DisposeAsync()
    {
        await Application.DisposeAsync();
        await _parent.DisposeAsync();
    }

    private static Shipment Shipment(int id, int merchantId, int? driverId, int? routeId) => new()
    {
        ShipmentId = id, MerchantId = merchantId, DriverId = driverId, RouteId = routeId,
        WarehouseId = 1, AddressId = 1, CodAmount = 100 + id
    };
}
