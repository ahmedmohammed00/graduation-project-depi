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
public class ProfileSecurityTests : IClassFixture<ProfileFixture>
{
    private readonly ProfileFixture fixture;
    public ProfileSecurityTests(ProfileFixture fixture)
    {
        this.fixture = fixture;
        fixture.Reset();
    }

    [Theory]
    [InlineData("Merchant", "Original shop", true)]
    [InlineData("SuperAdmin", "Administrator", false)]
    [InlineData("Dispatcher", "Dispatcher person", false)]
    [InlineData("Employee", "Staff person", false)]
    [InlineData("Driver", "Driver person", true)]
    public async Task ProfileShowsOnlyCurrentAccountAndApplicableFields(string role, string name, bool phone)
    {
        using var client = fixture.Client(role);
        using var response = await client.GetAsync("/Profile");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadAsStringAsync();
        Assert.Contains(name, page);
        Assert.Equal(phone, page.Contains("name=\"Details.Phone\""));
        Assert.Equal(role == "Merchant", page.Contains("name=\"Details.ContactName\""));
        Assert.DoesNotContain("Other shop", page);
        Assert.DoesNotContain(ProfileFixture.Hash, page);
        Assert.DoesNotContain(ProfileFixture.Password, page);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Contains("href=\"/Profile\"", page);
    }

    [Fact]
    public async Task AnonymousReadsAndWritesRequireAuthentication()
    {
        using var client = fixture.Client((string?)null);
        foreach (var path in new[] { "/Profile", "/Profile/Edit", "/Profile/ChangePassword" })
        {
            using var response = path == "/Profile" ? await client.GetAsync(path) : await client.PostAsync(path, Form(new()));
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Account/Login", response.Headers.Location?.OriginalString);
        }
    }

    [Theory]
    [InlineData("Edit")]
    [InlineData("ChangePassword")]
    public async Task PostsWithoutAntiforgeryCannotSave(string action)
    {
        using var client = fixture.Client("Merchant");
        using var response = await client.PostAsync("/Profile/" + action, Form(new()));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MerchantEditIgnoresOtherAccountIdsRolesAndCredentialFields()
    {
        using var client = fixture.Client("Merchant");
        var form = Details("  Updated shop  ", "merchant@example.invalid");
        form["Details.ContactName"] = "  New contact  ";
        form["Details.Phone"] = "+201012345678";
        foreach (var prefix in new[] { "", "Details." })
        {
            form[prefix + "MerchantId"] = "2";
            form[prefix + "EmployeeId"] = "1";
            form[prefix + "RoleId"] = "1";
            form[prefix + "Status"] = "suspended";
            form[prefix + "Password"] = "forged-password";
        }
        using var response = await Post(client, "Edit", form);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = fixture.Application.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DmsDbContext>();
        var own = db.Merchants.Single(m => m.MerchantId == 1);
        Assert.Equal("Updated shop", own.ShopName);
        Assert.Equal("New contact", own.ContactName);
        Assert.Equal("+201012345678", own.Phone);
        Assert.Equal("active", own.Status);
        Assert.Equal(ProfileFixture.Hash, own.Password);
        Assert.Equal("Other shop", db.Merchants.Single(m => m.MerchantId == 2).ShopName);
        Assert.Equal("Administrator", db.Employees.Single(e => e.EmployeeId == 1).Name);
        AssertRefreshCookie(response, client, "Updated shop", "Merchant");
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Contains("Updated shop", await client.GetStringAsync("/Profile"));
    }

    [Theory]
    [InlineData("SuperAdmin", 1)]
    [InlineData("Dispatcher", 2)]
    [InlineData("Employee", 4)]
    public async Task StaffCanEditTheirOwnNameWithoutChangingRoleOrPassword(string role, int id)
    {
        using var client = fixture.Client(role);
        var email = role.ToLowerInvariant() + "@example.invalid";
        var form = Details("Updated person", email);
        form["Details.RoleId"] = "1";
        form["Details.AccountStatus"] = "disabled";
        using var response = await Post(client, "Edit", form);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = fixture.Application.Services.CreateScope();
        var employee = scope.ServiceProvider.GetRequiredService<DmsDbContext>().Employees.Include(e => e.Role).Single(e => e.EmployeeId == id);
        Assert.Equal("Updated person", employee.Name);
        Assert.Equal(role, employee.Role!.RoleName);
        Assert.Equal("active", employee.AccountStatus);
        Assert.Equal(ProfileFixture.Hash, employee.Password);
        AssertRefreshCookie(response, client, "Updated person", role);
    }

    [Fact]
    public async Task DriverEditUsesEmployeeLinkAndSynchronizesNames()
    {
        using var client = fixture.Client("Driver");
        var form = Details("Updated driver", "driver@example.invalid");
        form["Details.Phone"] = "+201011111111";
        form["Details.DriverId"] = "22";
        form["Details.ZoneId"] = "99";
        form["Details.LicenseNumber"] = "forged";
        using var response = await Post(client, "Edit", form);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = fixture.Application.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DmsDbContext>();
        Assert.Equal("Updated driver", db.Employees.Single(e => e.EmployeeId == 3).Name);
        var driver = db.Drivers.Single(d => d.DriverId == 21);
        Assert.Equal("Updated driver", driver.Name);
        Assert.Equal("+201011111111", driver.PhoneNumber);
        Assert.Equal("original-license", driver.LicenseNumber);
        Assert.Null(driver.ZoneId);
        Assert.Equal("Other driver", db.Drivers.Single(d => d.DriverId == 22).Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("wrong-password")]
    public async Task EmailChangeRequiresCurrentPasswordAndDoesNotPartiallySave(string password)
    {
        using var client = fixture.Client("Merchant");
        var form = Details("Must not save", "new@example.invalid");
        form["Details.CurrentPassword"] = password;
        using var response = await Post(client, "Edit", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadAsStringAsync();
        Assert.Contains("Enter your current password", page);
        if (password.Length > 0) Assert.DoesNotContain(password, page);
        Assert.Contains("value=\"new@example.invalid\"", page);
        using var scope = fixture.Application.Services.CreateScope();
        var own = scope.ServiceProvider.GetRequiredService<DmsDbContext>().Merchants.Single(m => m.MerchantId == 1);
        Assert.Equal("Original shop", own.ShopName);
        Assert.Equal("merchant@example.invalid", own.Email);
    }

    [Theory]
    [InlineData("OTHER@EXAMPLE.INVALID")]
    [InlineData("DISPATCHER@EXAMPLE.INVALID")]
    public async Task EmailMustBeUniqueAcrossEmployeeAndMerchantAccountsIgnoringCase(string email)
    {
        using var client = fixture.Client("Merchant");
        var form = Details("Original shop", email);
        form["Details.CurrentPassword"] = ProfileFixture.Password;
        using var response = await Post(client, "Edit", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("This email address is already in use", await response.Content.ReadAsStringAsync());
        using var scope = fixture.Application.Services.CreateScope();
        Assert.Equal("merchant@example.invalid", scope.ServiceProvider.GetRequiredService<DmsDbContext>().Merchants.Single(m => m.MerchantId == 1).Email);
    }

    [Theory]
    [InlineData("Merchant")]
    [InlineData("Dispatcher")]
    public async Task EmailChangeUpdatesLoginAndSignsOutCurrentBrowser(string role)
    {
        using var client = fixture.Client(role);
        var form = Details("Updated name", "new@example.invalid");
        form["Details.CurrentPassword"] = ProfileFixture.Password;
        using var response = await Post(client, "Edit", form);
        Assert.Equal("/Account/Login", response.Headers.Location?.OriginalString);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("jwt=;") && c.Contains("expires="));
        var login = await client.GetStringAsync("/Account/Login");
        Assert.Contains("Sign in with your new email address", login);
        using var scope = fixture.Application.Services.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IAccountService>();
        Assert.NotNull(await accounts.AuthenticateUserAsync(new LoginViewModel { Email = "new@example.invalid", Password = ProfileFixture.Password }));
        Assert.Null(await accounts.AuthenticateUserAsync(new LoginViewModel { Email = role.ToLowerInvariant() + "@example.invalid", Password = ProfileFixture.Password }));
    }

    [Theory]
    [InlineData("wrong-password", "New-password-123", "New-password-123", "Your current password is incorrect")]
    [InlineData(ProfileFixture.Password, "short", "short", "minimum length of 8")]
    [InlineData(ProfileFixture.Password, "New-password-123", "Mismatch-password", "The passwords do not match")]
    [InlineData(ProfileFixture.Password, ProfileFixture.Password, ProfileFixture.Password, "different from your current password")]
    public async Task InvalidPasswordChangePreservesHashAndDoesNotEchoPasswords(string current, string next, string confirm, string error)
    {
        using var client = fixture.Client("Merchant");
        using var response = await Post(client, "ChangePassword", Passwords(current, next, confirm));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadAsStringAsync();
        Assert.Contains(error, page);
        foreach (var secret in new[] { current, next, confirm }) Assert.DoesNotContain("value=\"" + secret + "\"", page);
        using var scope = fixture.Application.Services.CreateScope();
        Assert.Equal(ProfileFixture.Hash, scope.ServiceProvider.GetRequiredService<DmsDbContext>().Merchants.Single(m => m.MerchantId == 1).Password);
    }

    [Theory]
    [InlineData("Merchant")]
    [InlineData("Driver")]
    public async Task PasswordChangeHashesNewPasswordAndOldPasswordNoLongerAuthenticates(string role)
    {
        using var client = fixture.Client(role);
        using var response = await Post(client, "ChangePassword", Passwords(ProfileFixture.Password, "New-password-123", "New-password-123"));
        Assert.Equal("/Account/Login", response.Headers.Location?.OriginalString);
        using var scope = fixture.Application.Services.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IAccountService>();
        var email = role.ToLowerInvariant() + "@example.invalid";
        Assert.Null(await accounts.AuthenticateUserAsync(new LoginViewModel { Email = email, Password = ProfileFixture.Password }));
        Assert.NotNull(await accounts.AuthenticateUserAsync(new LoginViewModel { Email = email, Password = "New-password-123" }));
        var db = scope.ServiceProvider.GetRequiredService<DmsDbContext>();
        var hash = role == "Merchant" ? db.Merchants.Single(m => m.MerchantId == 1).Password : db.Employees.Single(e => e.EmployeeId == 3).Password;
        Assert.StartsWith(PasswordHashing.EnhancedPrefix, hash);
        Assert.NotEqual(ProfileFixture.Hash, hash);
        Assert.Equal(ProfileFixture.Hash, db.Merchants.Single(m => m.MerchantId == 2).Password);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("mismatched-merchant")]
    [InlineData("mismatched-driver")]
    [InlineData("mixed-roles")]
    [InlineData("unknown-role")]
    public async Task AmbiguousOrInvalidIdentityCannotResolveAnyProfile(string variant)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, "Merchant"), new(ClaimTypes.NameIdentifier, "1"), new("MerchantId", "1") };
        if (variant == "missing") claims.RemoveAll(c => c.Type == ClaimTypes.NameIdentifier);
        if (variant == "duplicate") claims.Add(new(ClaimTypes.NameIdentifier, "2"));
        if (variant == "mismatched-merchant") { claims.RemoveAll(c => c.Type == "MerchantId"); claims.Add(new("MerchantId", "2")); }
        if (variant == "mismatched-driver") claims.Add(new("DriverId", "22"));
        if (variant == "mixed-roles") claims.Add(new(ClaimTypes.Role, "SuperAdmin"));
        if (variant == "unknown-role") { claims.Clear(); claims.AddRange(new[] { new Claim(ClaimTypes.Role, "Unknown"), new Claim(ClaimTypes.NameIdentifier, "1") }); }
        using var client = fixture.Client(claims);
        using var response = await client.GetAsync("/Profile");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var scope = fixture.Application.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IProfileService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        Assert.False((await service.UpdateAsync(principal, new ProfileEditViewModel { Name = "forged", Email = "merchant@example.invalid" })).Found);
        Assert.False((await service.ChangePasswordAsync(principal, new ChangePasswordViewModel())).Found);
    }

    private async Task<HttpResponseMessage> Post(HttpClient client, string action, Dictionary<string, string> fields)
    {
        var page = await client.GetStringAsync("/Profile");
        var match = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success);
        fields["__RequestVerificationToken"] = WebUtility.HtmlDecode(match.Groups[1].Value);
        return await client.PostAsync("/Profile/" + action, Form(fields));
    }

    private static void AssertRefreshCookie(HttpResponseMessage response, HttpClient client, string name, string role)
    {
        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("jwt="));
        Assert.Contains("secure", cookie); Assert.Contains("httponly", cookie); Assert.Contains("samesite=strict", cookie);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(cookie.Split(';')[0][4..]);
        var original = new JwtSecurityTokenHandler().ReadJwtToken(client.DefaultRequestHeaders.Authorization!.Parameter);
        Assert.Equal(original.ValidTo, token.ValidTo);
        Assert.Equal(name, token.Claims.Single(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal(role, token.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
    }

    private static Dictionary<string, string> Details(string name, string email) => new() { ["Details.Name"] = name, ["Details.Email"] = email };
    private static Dictionary<string, string> Passwords(string current, string next, string confirm) => new()
    {
        ["Password.CurrentPassword"] = current, ["Password.NewPassword"] = next, ["Password.ConfirmPassword"] = confirm
    };
    private static FormUrlEncodedContent Form(Dictionary<string, string> fields) => new(fields);
}

public sealed class ProfileFixture : IDisposable
{
    public const string Password = "Original-password-123";
    public static readonly string Hash = PasswordHashing.HashNewPassword(Password);
    private readonly ManagementApplicationFactory root = new();
    public WebApplicationFactory<Program> Application { get; }

    public ProfileFixture()
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
        db.Database.EnsureDeleted();
        db.Roles.AddRange(new Role { RoleId = 1, RoleName = "SuperAdmin" }, new Role { RoleId = 2, RoleName = "Dispatcher" },
            new Role { RoleId = 3, RoleName = "Driver" }, new Role { RoleId = 4, RoleName = "Employee" });
        var roles = new[] { "SuperAdmin", "Dispatcher", "Driver", "Employee" };
        var names = new[] { "Administrator", "Dispatcher person", "Driver person", "Staff person" };
        for (var i = 0; i < 4; i++) db.Employees.Add(new Employee { EmployeeId = i + 1, RoleId = i + 1,
            Name = names[i], Email = roles[i].ToLowerInvariant() + "@example.invalid", Password = Hash });
        db.Merchants.AddRange(new Merchant { MerchantId = 1, ShopName = "Original shop", Email = "merchant@example.invalid", Password = Hash },
            new Merchant { MerchantId = 2, ShopName = "Other shop", Email = "other@example.invalid", Password = Hash });
        db.Drivers.AddRange(new Driver { DriverId = 21, EmployeeId = 3, Name = "Driver person", LicenseNumber = "original-license" },
            new Driver { DriverId = 22, Name = "Other driver" });
        db.SaveChanges();
    }

    public HttpClient Client(string? role)
    {
        if (role == null) return Client(Array.Empty<Claim>());
        var id = role switch { "Merchant" => "1", "SuperAdmin" => "1", "Dispatcher" => "2", "Driver" => "3", _ => "4" };
        var claims = new List<Claim> { new(ClaimTypes.Role, role), new(ClaimTypes.NameIdentifier, id), new(ClaimTypes.Name, "Original name") };
        if (role == "Merchant") claims.Add(new("MerchantId", id));
        if (role == "Driver") claims.Add(new("DriverId", "21"));
        return Client(claims);
    }

    public HttpClient Client(IEnumerable<Claim> claims)
    {
        var client = Application.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        if (claims.Any())
        {
            var settings = Application.Services.GetRequiredService<JwtSettings>();
            var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims, expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: new SigningCredentials(settings.SigningKey, SecurityAlgorithms.HmacSha256));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        }
        return client;
    }

    public void Dispose() { Application.Dispose(); root.Dispose(); }
}
