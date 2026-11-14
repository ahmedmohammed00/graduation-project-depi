using System.ComponentModel.DataAnnotations;
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

public class SignupSecurityTests : IClassFixture<ManagementApplicationFactory>
{
    private const string Password = "Audit-only-password-123";
    private readonly ManagementApplicationFactory _application;

    public SignupSecurityTests(ManagementApplicationFactory application) => _application = application;

    [Theory]
    [InlineData("dispatcher")]
    [InlineData("driver")]
    [InlineData("employee")]
    [InlineData("SuperAdmin")]
    [InlineData("Dispatcher")]
    [InlineData("Driver")]
    [InlineData("Merchant")]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData(null)]
    public async Task PublicRegistrationRejectsNonMerchantRolesAtValidationAndServiceBoundaries(string? role)
    {
        using var db = NewDatabase();
        var model = ValidSignup();
        model.Role = role!;
        var errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(model, new ValidationContext(model), errors, true));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(SignupViewModel.Role)));

        await Assert.ThrowsAsync<ValidationException>(() => new AccountService(db).CreateUserAsync(model));
        Assert.Empty(db.Employees);
        Assert.Empty(db.Drivers);
        Assert.Empty(db.Merchants);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task MerchantSignupRequiresShopName(string? shopName)
    {
        using var db = NewDatabase();
        var model = ValidSignup();
        model.ShopName = shopName!;
        await Assert.ThrowsAsync<ValidationException>(() => new AccountService(db).CreateUserAsync(model));
        Assert.Empty(db.Merchants);
    }

    [Fact]
    public async Task DefaultSignupCreatesOnlyAMerchantWithAHashedPassword()
    {
        using var db = NewDatabase();
        var model = ValidSignup();
        Assert.Equal("merchant", model.Role);
        var service = new AccountService(db);
        await service.CreateUserAsync(model);

        var merchant = Assert.Single(db.Merchants);
        Assert.StartsWith(PasswordHashing.EnhancedPrefix, merchant.Password);
        Assert.True(BCrypt.Net.BCrypt.EnhancedVerify(Password, merchant.Password[PasswordHashing.EnhancedPrefix.Length..]));
        Assert.NotEqual(Password, merchant.Password);
        Assert.Empty(db.Employees);
        Assert.Empty(db.Drivers);
        var claims = await service.AuthenticateUserAsync(new LoginViewModel { Email = model.Email, Password = Password });
        Assert.Equal(new[] { "Merchant" }, claims!.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value));
    }

    [Fact]
    public async Task EmployeeWithoutAssignedRoleCannotAcquireEmployeePrivileges()
    {
        using var db = NewDatabase();
        db.Employees.Add(Employee());
        await db.SaveChangesAsync();
        Assert.Null(await new AccountService(db).AuthenticateUserAsync(Login()));
    }

    [Theory]
    [InlineData("SuperAdmin")]
    [InlineData("Dispatcher")]
    [InlineData("Employee")]
    public async Task ExplicitlyAssignedStaffRoleCanStillAuthenticate(string roleName)
    {
        using var db = NewDatabase();
        var employee = Employee();
        employee.Role = new Role { RoleName = roleName };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        var claims = await new AccountService(db).AuthenticateUserAsync(Login());
        Assert.Equal(new[] { roleName }, claims!.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value));
    }

    [Fact]
    public async Task LinkedDriverWithoutStaffRoleReceivesOnlyDriverAccess()
    {
        using var db = NewDatabase();
        var employee = Employee();
        db.Drivers.Add(new Driver { Employee = employee, Name = employee.Name });
        await db.SaveChangesAsync();

        var claims = await new AccountService(db).AuthenticateUserAsync(Login());
        Assert.NotNull(claims);
        Assert.Equal(new[] { "Driver" }, claims!.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value));
        Assert.Contains(claims, c => c.Type == "DriverId");
    }

    [Fact]
    public async Task DriverRoleWithoutDriverRecordCannotAuthenticate()
    {
        using var db = NewDatabase();
        var employee = Employee();
        employee.Role = new Role { RoleName = "Driver" };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        Assert.Null(await new AccountService(db).AuthenticateUserAsync(Login()));
    }

    [Theory]
    [InlineData("driver")]
    [InlineData("dispatcher")]
    [InlineData("SuperAdmin")]
    [InlineData("Employee")]
    [InlineData("")]
    public async Task ForgedStaffSignupWithValidAntiforgeryDoesNotCreateAnyAccount(string role)
    {
        using var application = IsolatedSignupApplication();
        using var client = application.CreateClient(ClientOptions());
        var form = SignupForm(await AntiforgeryToken(client));
        form["Role"] = role;
        using var response = await client.PostAsync("/Account/Signup", new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("validation-summary-errors", await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain(response.Headers.GetValuesOrEmpty("Set-Cookie"), value => value.StartsWith("jwt="));
        using var scope = application.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DmsDbContext>();
        Assert.Empty(db.Employees);
        Assert.Empty(db.Drivers);
        Assert.Empty(db.Merchants);
    }

    [Fact]
    public async Task SignupWithoutRoleCreatesMerchantAndIssuesOnlyMerchantClaims()
    {
        using var application = IsolatedSignupApplication();
        using var client = application.CreateClient(ClientOptions());
        var form = SignupForm(await AntiforgeryToken(client));
        // Try extra privilege fields that the public view model must not bind.
        form["RoleId"] = "1";
        form["Employee.RoleId"] = "1";
        using var response = await client.PostAsync("/Account/Signup", new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/MerchantPortal", response.Headers.Location!.OriginalString);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), value => value.StartsWith("jwt="));
        var token = cookie.Split(';', 2)[0]["jwt=".Length..];
        var settings = application.Services.GetRequiredService<JwtSettings>();
        var principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
            ValidIssuer = settings.Issuer, ValidAudience = settings.Audience, IssuerSigningKey = settings.SigningKey
        }, out _);
        Assert.Equal(new[] { "Merchant" }, principal.FindAll(ClaimTypes.Role).Select(c => c.Value));

        using var scope = application.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DmsDbContext>();
        Assert.Single(db.Merchants);
        Assert.Empty(db.Employees);
        Assert.Empty(db.Drivers);
    }

    [Fact]
    public async Task SignupPageDoesNotOfferStaffRoleSelection()
    {
        using var client = _application.CreateClient(ClientOptions());
        var page = await client.GetStringAsync("/Account/Signup");
        Assert.Contains("Create your merchant account", page);
        Assert.Contains("name=\"Role\" value=\"merchant\"", page);
        Assert.DoesNotContain("data-role=\"dispatcher\"", page);
        Assert.DoesNotContain("data-role=\"driver\"", page);
    }

    [Theory]
    [InlineData("Employee", "/Invoices/Index", "GET")]
    [InlineData("Employee", "/Invoices/Delete/1", "POST")]
    [InlineData("Employee", "/Shipments/Index", "GET")]
    [InlineData("Employee", "/Shipments/Edit/1", "POST")]
    [InlineData("Employee", "/Shipments/UpdateStatus/1", "POST")]
    [InlineData("Driver", "/Invoices/Index", "GET")]
    [InlineData("Driver", "/Shipments/Edit/1", "POST")]
    [InlineData("Driver", "/Shipments/Delete/1", "POST")]
    public async Task GeneralEmployeesAndDriversCannotUseStaffAdministration(string role, string path, string method)
    {
        using var client = _application.CreateClient(ClientOptions());
        Authenticate(client, role);
        using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("SuperAdmin", "/Invoices/Details")]
    [InlineData("SuperAdmin", "/Shipments/Edit")]
    [InlineData("Dispatcher", "/Shipments/Edit")]
    public async Task AuthorizedStaffStillReachAdministrationActions(string role, string path)
    {
        using var client = _application.CreateClient(ClientOptions());
        Authenticate(client, role);
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private WebApplicationFactory<Program> IsolatedSignupApplication()
    {
        var name = Guid.NewGuid().ToString();
        return _application.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<DmsDbContext>();
            services.AddScoped(_ => new DmsDbContext(new DbContextOptionsBuilder<DmsDbContext>()
                .UseInMemoryDatabase(name).Options));
        }));
    }

    private void Authenticate(HttpClient client, string role)
    {
        var settings = _application.Services.GetRequiredService<JwtSettings>();
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience,
            new[] { new Claim(ClaimTypes.Role, role) }, expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(settings.SigningKey, SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
    }

    private static WebApplicationFactoryClientOptions ClientOptions() => new()
    {
        BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
    };

    private static async Task<string> AntiforgeryToken(HttpClient client)
    {
        var page = await client.GetStringAsync("/Account/Signup");
        var match = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "Signup form must contain an antiforgery token.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static Dictionary<string, string> SignupForm(string antiforgery) => new()
    {
        ["__RequestVerificationToken"] = antiforgery,
        ["FullName"] = "Audit Merchant", ["Email"] = "audit@example.invalid", ["Phone"] = "01000000000",
        ["Password"] = Password, ["ConfirmPassword"] = Password, ["ShopName"] = "Audit Shop", ["ShopAddress"] = "Audit Address"
    };

    private static DmsDbContext NewDatabase() => new(new DbContextOptionsBuilder<DmsDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static Employee Employee() => new()
    {
        Name = "Audit Employee", Email = "audit@example.invalid", Password = BCrypt.Net.BCrypt.HashPassword(Password)
    };
    private static LoginViewModel Login() => new() { Email = "audit@example.invalid", Password = Password };
    private static SignupViewModel ValidSignup() => new()
    {
        FullName = "Audit Merchant", Email = "audit@example.invalid", Phone = "01000000000", Password = Password,
        ConfirmPassword = Password, ShopName = "Audit Shop"
    };
}

internal static class ResponseHeaderExtensions
{
    public static IEnumerable<string> GetValuesOrEmpty(this System.Net.Http.Headers.HttpHeaders headers, string name) =>
        headers.TryGetValues(name, out var values) ? values : Array.Empty<string>();
}
