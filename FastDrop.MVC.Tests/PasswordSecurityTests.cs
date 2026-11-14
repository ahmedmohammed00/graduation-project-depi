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

public class PasswordSecurityTests : IClassFixture<ManagementApplicationFactory>
{
    private const string Password = "Audit-original-password-123";
    private static readonly string OriginalHash = PasswordHashing.HashNewPassword(Password);
    private readonly ManagementApplicationFactory _application;
    public PasswordSecurityTests(ManagementApplicationFactory application) => _application = application;

    [Fact]
    public async Task ManagementCreationStoresSaltedHashesForMerchantsAndEmployees()
    {
        using var db = NewDatabase();
        await new MerchantService(db).CreateMerchantAsync(new MerchantCreateViewModel
        {
            ShopName = "Audit Shop", Email = "merchant@example.invalid", Password = Password
        });
        await new EmployeeService(db).CreateEmployeeAsync(new Employee
        {
            Name = "Audit Employee", Email = "employee@example.invalid", Password = Password
        });

        var merchantHash = db.Merchants.Single().Password;
        var employeeHash = db.Employees.Single().Password;
        Assert.StartsWith(PasswordHashing.EnhancedPrefix, merchantHash);
        Assert.StartsWith(PasswordHashing.EnhancedPrefix, employeeHash);
        Assert.NotEqual(Password, merchantHash);
        Assert.NotEqual(merchantHash, employeeHash);
        Assert.True(PasswordHashing.Verify(Password, merchantHash));
        Assert.True(PasswordHashing.Verify(Password, employeeHash));
    }

    [Fact]
    public void LongPasswordsAreNotSilentlyTruncated()
    {
        var prefix = new string('x', 80);
        var password = prefix + "original";
        var hash = PasswordHashing.HashNewPassword(password);
        Assert.True(PasswordHashing.Verify(password, hash));
        Assert.False(PasswordHashing.Verify(prefix + "different", hash));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("plaintext-password")]
    [InlineData("$2a$broken")]
    [InlineData("bcrypt-sha384:broken")]
    public void PlaintextAndMalformedStoredCredentialsAreRejected(string? storedPassword)
    {
        Assert.False(PasswordHashing.Verify(storedPassword ?? Password, storedPassword));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PlaintextStoredPasswordCannotAuthenticate(bool employeeAccount)
    {
        using var db = NewDatabase();
        if (employeeAccount)
            db.Employees.Add(new Employee { Name = "Audit", Email = "audit@example.invalid", Password = Password,
                Role = new Role { RoleName = "SuperAdmin" } });
        else
            db.Merchants.Add(new Merchant { ShopName = "Audit", Email = "audit@example.invalid", Password = Password });
        await db.SaveChangesAsync();
        Assert.Null(await new AccountService(db).AuthenticateUserAsync(new LoginViewModel
        {
            Email = "audit@example.invalid", Password = Password
        }));
    }

    [Fact]
    public async Task ExistingStandardBcryptHashStillAuthenticates()
    {
        using var db = NewDatabase();
        var hash = BCrypt.Net.BCrypt.HashPassword(Password);
        db.Merchants.Add(new Merchant { ShopName = "Audit", Email = "audit@example.invalid", Password = hash });
        await db.SaveChangesAsync();
        var claims = await new AccountService(db).AuthenticateUserAsync(new LoginViewModel
        {
            Email = "audit@example.invalid", Password = Password
        });
        Assert.NotNull(claims);
        Assert.Equal(hash, db.Merchants.Single().Password);
    }

    [Fact]
    public async Task LegacyMigrationPreservesHashesAndIsIdempotent()
    {
        using var db = NewDatabase();
        var standardHash = BCrypt.Net.BCrypt.HashPassword(Password);
        db.Employees.Add(new Employee { Name = "Legacy", Email = "legacy@example.invalid", Password = "short",
            Role = new Role { RoleName = "Dispatcher" } });
        db.Merchants.AddRange(
            new Merchant { ShopName = "Legacy", Email = "plain@example.invalid", Password = Password },
            new Merchant { ShopName = "Standard", Email = "standard@example.invalid", Password = standardHash },
            new Merchant { ShopName = "Enhanced", Email = "enhanced@example.invalid", Password = OriginalHash },
            new Merchant { ShopName = "Damaged", Email = "damaged@example.invalid", Password = "$2a$broken" });
        await db.SaveChangesAsync();

        var migration = new CredentialMigrationService(db);
        var result = await migration.MigrateLegacyPasswordsAsync();
        Assert.Equal((1, 1, 1), result);
        Assert.True(PasswordHashing.Verify("short", db.Employees.Single().Password));
        Assert.True(PasswordHashing.Verify(Password, db.Merchants.Single(m => m.ShopName == "Legacy").Password));
        Assert.Equal(standardHash, db.Merchants.Single(m => m.ShopName == "Standard").Password);
        Assert.Equal(OriginalHash, db.Merchants.Single(m => m.ShopName == "Enhanced").Password);
        Assert.Equal("$2a$broken", db.Merchants.Single(m => m.ShopName == "Damaged").Password);
        Assert.Equal((0, 0, 1), await migration.MigrateLegacyPasswordsAsync());
        Assert.NotNull(await new AccountService(db).AuthenticateUserAsync(new LoginViewModel
        {
            Email = "plain@example.invalid", Password = Password
        }));
    }

    [Fact]
    public async Task MigrationContinuesBeyondTheFirstBatch()
    {
        using var db = NewDatabase();
        for (var i = 1; i <= 101; i++)
            db.Employees.Add(new Employee { EmployeeId = i, Name = "Audit", Email = $"{i}@example.invalid",
                Password = i == 101 ? Password : OriginalHash });
        await db.SaveChangesAsync();
        Assert.Equal((1, 0, 0), await new CredentialMigrationService(db).MigrateLegacyPasswordsAsync());
        Assert.True(PasswordHashing.Verify(Password, db.Employees.Single(e => e.EmployeeId == 101).Password));
    }

    [Theory]
    [InlineData("/Merchants/Details/1")]
    [InlineData("/Merchants/Edit/1")]
    [InlineData("/AdminPortal/EditEmployee/1")]
    public async Task ManagementPagesDoNotExposeCredentials(string path)
    {
        using var application = IsolatedApplication();
        await Seed(application);
        using var client = AdminClient(application);
        var page = await client.GetStringAsync(path);
        Assert.DoesNotContain(OriginalHash, page);
        Assert.DoesNotContain("name=\"Password\"", page);
        Assert.DoesNotContain(Password, page);
    }

    [Fact]
    public async Task MerchantProfileEditIgnoresForgedPasswordFields()
    {
        using var application = IsolatedApplication();
        await Seed(application);
        using var client = AdminClient(application);
        var token = await AntiforgeryToken(client, "/Merchants/Edit/1");
        using var response = await client.PostAsync("/Merchants/Edit/1", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token, ["MerchantId"] = "1", ["ShopName"] = "Changed shop",
            ["Email"] = "merchant@example.invalid", ["Status"] = "active", ["Password"] = "Attacker-chosen-password"
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = application.Services.CreateScope();
        var merchant = scope.ServiceProvider.GetRequiredService<DmsDbContext>().Merchants.Single();
        Assert.Equal("Changed shop", merchant.ShopName);
        Assert.Equal(OriginalHash, merchant.Password);
    }

    [Fact]
    public async Task EmployeeRoleEditCannotChangeCredentialsOrProfileFields()
    {
        using var application = IsolatedApplication();
        await Seed(application);
        using var client = AdminClient(application);
        var token = await AntiforgeryToken(client, "/AdminPortal/EditEmployee/1");
        using var response = await client.PostAsync("/AdminPortal/EditEmployee/1", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token, ["EmployeeId"] = "1", ["RoleId"] = "2",
            ["Password"] = "Attacker-chosen-password", ["Email"] = "attacker@example.invalid",
            ["Name"] = "Forged", ["AccountStatus"] = "inactive", ["SupervisorId"] = "999"
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = application.Services.CreateScope();
        var employee = scope.ServiceProvider.GetRequiredService<DmsDbContext>().Employees.Single();
        Assert.Equal(2, employee.RoleId);
        Assert.Equal(OriginalHash, employee.Password);
        Assert.Equal("employee@example.invalid", employee.Email);
        Assert.Equal("active", employee.AccountStatus);
        Assert.Null(employee.SupervisorId);
    }

    [Fact]
    public async Task MerchantCreationThroughHttpHashesPasswordAndRemainsUsableForLogin()
    {
        using var application = IsolatedApplication();
        using var client = AdminClient(application);
        var token = await AntiforgeryToken(client, "/Merchants/Create");
        using var response = await client.PostAsync("/Merchants/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token, ["ShopName"] = "Audit Shop",
            ["Email"] = "new@example.invalid", ["Password"] = Password, ["Status"] = "active"
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = application.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DmsDbContext>();
        Assert.NotEqual(Password, db.Merchants.Single().Password);
        Assert.NotNull(await new AccountService(db).AuthenticateUserAsync(new LoginViewModel
        {
            Email = "new@example.invalid", Password = Password
        }));
    }

    private WebApplicationFactory<Program> IsolatedApplication()
    {
        var name = Guid.NewGuid().ToString();
        return _application.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<DmsDbContext>();
            services.AddScoped(_ => new DmsDbContext(new DbContextOptionsBuilder<DmsDbContext>()
                .UseInMemoryDatabase(name).Options));
        }));
    }

    private static async Task Seed(WebApplicationFactory<Program> application)
    {
        using var scope = application.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DmsDbContext>();
        db.Roles.AddRange(new Role { RoleId = 1, RoleName = "SuperAdmin" }, new Role { RoleId = 2, RoleName = "Dispatcher" });
        db.Employees.Add(new Employee { EmployeeId = 1, RoleId = 1, Name = "Audit Employee",
            Email = "employee@example.invalid", Password = OriginalHash });
        db.Merchants.Add(new Merchant { MerchantId = 1, ShopName = "Audit Shop", Email = "merchant@example.invalid",
            Password = OriginalHash, Address = new Address { Street = "Audit Street" } });
        await db.SaveChangesAsync();
    }

    private static HttpClient AdminClient(WebApplicationFactory<Program> application)
    {
        var client = application.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
        });
        var settings = application.Services.GetRequiredService<JwtSettings>();
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience,
            new[] { new Claim(ClaimTypes.Role, "SuperAdmin") }, expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(settings.SigningKey, SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }

    private static async Task<string> AntiforgeryToken(HttpClient client, string path)
    {
        var page = await client.GetStringAsync(path);
        var match = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success);
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static DmsDbContext NewDatabase() => new(new DbContextOptionsBuilder<DmsDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
