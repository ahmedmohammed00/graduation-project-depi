using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.RegularExpressions;
using FastDrop.MVC.Models;
using FastDrop.MVC.Security;
using FastDrop.MVC.Services;
using FastDrop.MVC.ViewModels;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace FastDrop.MVC.Tests;

[Collection("Shipment read HTTP host")]
public class SessionSecurityTests : IClassFixture<SessionFixture>
{
    private readonly SessionFixture fixture;
    public SessionSecurityTests(SessionFixture fixture) { this.fixture = fixture; fixture.Reset(); }

    [Theory]
    [InlineData("Merchant")]
    [InlineData("SuperAdmin")]
    [InlineData("Driver")]
    public async Task RealProductionSessionValidationAcceptsFreshAuthenticatedAccounts(string role)
    {
        using var scope = fixture.Application.Services.CreateScope();
        Assert.IsType<AccountSessionValidator>(scope.ServiceProvider.GetRequiredService<IAccountSessionValidator>());
        using var client = fixture.Client(await fixture.Token(role));
        using var response = await client.GetAsync("/Profile");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("Merchant", "inactive")]
    [InlineData("Merchant", "pending")]
    [InlineData("Employee", "disabled")]
    [InlineData("Employee", "suspended")]
    public async Task InactiveAccountsCannotAuthenticateOrContinueUsingIssuedTokens(string type, string status)
    {
        var role = type == "Merchant" ? "Merchant" : "SuperAdmin";
        var oldToken = await fixture.Token(role);
        await fixture.Mutate(db => { if (type == "Merchant") db.Merchants.Single().Status = status; else db.Employees.Single(e => e.EmployeeId == 1).AccountStatus = status; });
        await fixture.InScope(async services => Assert.Null(await services.GetRequiredService<IAccountService>().AuthenticateUserAsync(new LoginViewModel
        {
            Email = type == "Merchant" ? "merchant@example.invalid" : "admin@example.invalid", Password = SessionFixture.Password
        })));
        using var client = fixture.Client(oldToken);
        AssertChallenge(await client.GetAsync("/authorization-test/protected"));
    }

    [Theory]
    [InlineData("password")]
    [InlineData("email")]
    [InlineData("role")]
    [InlineData("disable-enable")]
    public async Task CredentialAndRoleChangesInvalidatePreviouslyIssuedTokens(string change)
    {
        var oldToken = await fixture.Token("SuperAdmin");
        await fixture.Mutate(db =>
        {
            var employee = db.Employees.Single(e => e.EmployeeId == 1);
            if (change == "password") employee.Password = PasswordHashing.HashNewPassword("Changed-password-123");
            if (change == "email") employee.Email = "changed@example.invalid";
            if (change == "role") employee.RoleId = 2;
            if (change == "disable-enable") employee.AccountStatus = "disabled";
        });
        if (change == "disable-enable") await fixture.Mutate(db => db.Employees.Single(e => e.EmployeeId == 1).AccountStatus = "active");
        using var client = fixture.Client(oldToken);
        AssertChallenge(await client.GetAsync("/authorization-test/protected"));
    }

    [Fact]
    public async Task CurrentRolesAreRecheckedEvenIfRoleDefinitionChangesWithoutAnEmployeeUpdate()
    {
        var token = await fixture.Token("SuperAdmin");
        await fixture.Mutate(db => db.Roles.Single(r => r.RoleId == 1).RoleName = "Dispatcher");
        using var client = fixture.Client(token);
        AssertChallenge(await client.GetAsync("/authorization-test/protected"));
    }

    [Fact]
    public async Task DriverLinksAreRevalidatedAgainstTheAccount()
    {
        var token = await fixture.Token("Driver");
        await fixture.Mutate(db => db.Drivers.Single().EmployeeId = null);
        using var client = fixture.Client(token);
        AssertChallenge(await client.GetAsync("/authorization-test/protected"));
    }

    [Theory]
    [InlineData("missing-stamp")]
    [InlineData("duplicate-stamp")]
    [InlineData("wrong-type")]
    [InlineData("wrong-owner")]
    public async Task LegacyAndAmbiguousSignedTokensCannotBypassSessionValidation(string variant)
    {
        var claims = await fixture.Claims("Merchant");
        if (variant == "missing-stamp") claims.RemoveAll(c => c.Type == AccountSessionValidator.StampClaim);
        if (variant == "duplicate-stamp") claims.Add(new(AccountSessionValidator.StampClaim, Guid.NewGuid().ToString()));
        if (variant == "wrong-type") { claims.RemoveAll(c => c.Type == AccountSessionValidator.TypeClaim); claims.Add(new(AccountSessionValidator.TypeClaim, "Employee")); }
        if (variant == "wrong-owner") { claims.RemoveAll(c => c.Type == "MerchantId"); claims.Add(new("MerchantId", "2")); }
        using var client = fixture.Client(fixture.Encode(claims));
        AssertChallenge(await client.GetAsync("/authorization-test/protected"));
    }

    [Fact]
    public async Task LogoutRequiresAProtectedPostAndRevokesCopiedTokens()
    {
        var token = await fixture.Token("Merchant");
        using var client = fixture.Client(token);
        using var get = await client.GetAsync("/Account/Logout");
        Assert.Contains(get.StatusCode, new[] { HttpStatusCode.MethodNotAllowed, HttpStatusCode.NotFound });
        using var noCsrf = await client.PostAsync("/Account/Logout", new FormUrlEncodedContent(new Dictionary<string, string>()));
        Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);
        using var before = await client.GetAsync("/Profile"); Assert.Equal(HttpStatusCode.OK, before.StatusCode);
        var form = new Dictionary<string, string> { ["__RequestVerificationToken"] = await fixture.Antiforgery(client) };
        using var logout = await client.PostAsync("/Account/Logout", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        using var copied = fixture.Client(token);
        AssertChallenge(await copied.GetAsync("/authorization-test/protected"));
    }

    [Fact]
    public async Task ProfilePasswordChangeRevokesTokensInOtherBrowsers()
    {
        var token = await fixture.Token("Merchant");
        using var client = fixture.Client(token);
        using var response = await client.PostAsync("/Profile/ChangePassword", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = await fixture.Antiforgery(client),
            ["Password.CurrentPassword"] = SessionFixture.Password, ["Password.NewPassword"] = "New-password-456",
            ["Password.ConfirmPassword"] = "New-password-456"
        }));
        Assert.Equal("/Account/Login", response.Headers.Location?.OriginalString);
        using var copied = fixture.Client(token);
        AssertChallenge(await copied.GetAsync("/authorization-test/protected"));
        using var fresh = fixture.Client(await fixture.Token("Merchant", "New-password-456"));
        Assert.Equal(HttpStatusCode.OK, (await fresh.GetAsync("/Profile")).StatusCode);
    }

    [Fact]
    public async Task OrdinaryProfileEditsKeepSessionValidWithoutExtendingItsExpiry()
    {
        var token = await fixture.Token("Merchant");
        using var client = fixture.Client(token);
        using var response = await client.PostAsync("/Profile/Edit", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = await fixture.Antiforgery(client),
            ["Details.Name"] = "Updated shop", ["Details.Email"] = "merchant@example.invalid"
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("jwt="));
        var fresh = cookie.Split(';')[0][4..];
        Assert.Equal(new JwtSecurityTokenHandler().ReadJwtToken(token).ValidTo, new JwtSecurityTokenHandler().ReadJwtToken(fresh).ValidTo);
        using var refreshed = fixture.Client(fresh);
        Assert.Equal(HttpStatusCode.OK, (await refreshed.GetAsync("/Profile")).StatusCode);
    }

    [Fact]
    public async Task LoginIssuesShortLivedTokensThatPassTheProductionValidator()
    {
        using var client = fixture.Client();
        var page = await client.GetStringAsync("/Account/Login");
        var csrf = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        using var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(csrf), ["Email"] = "merchant@example.invalid", ["Password"] = SessionFixture.Password
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("jwt="));
        var encoded = cookie.Split(';')[0][4..];
        var token = new JwtSecurityTokenHandler().ReadJwtToken(encoded);
        Assert.InRange(token.ValidTo, DateTime.UtcNow.AddMinutes(29), DateTime.UtcNow.AddMinutes(31));
        Assert.Contains(token.Claims, c => c.Type == AccountSessionValidator.StampClaim);
        using var authenticated = fixture.Client(encoded);
        Assert.Equal(HttpStatusCode.OK, (await authenticated.GetAsync("/Profile")).StatusCode);
    }

    [Fact]
    public async Task PaymentMutationRequiresAntiforgeryEvenForAnAdministrator()
    {
        using var client = fixture.Client(await fixture.Token("SuperAdmin"));
        using var denied = await client.PostAsync("/AdminPortal/MarkPaymentPaid/1", new FormUrlEncodedContent(new Dictionary<string, string>()));
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        using var allowed = await client.PostAsync("/AdminPortal/MarkPaymentPaid/1", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = await fixture.Antiforgery(client)
        }));
        Assert.Equal(HttpStatusCode.Redirect, allowed.StatusCode);
    }

    [Theory]
    [InlineData("Login")]
    [InlineData("Signup")]
    public async Task AccountEndpointsActuallyApplyThrottlingBeforeExecutingFurtherPosts(string action)
    {
        using var application = fixture.Application.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.PostConfigure<AccountThrottleOptions>(o => { o.LoginAccountLimit = o.SignupAccountLimit = 1; })));
        using var client = application.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var page = await client.GetStringAsync("/Account/" + action);
        var csrf = WebUtility.HtmlDecode(Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        var fields = new Dictionary<string, string> { ["__RequestVerificationToken"] = csrf, ["Email"] = "merchant@example.invalid" };
        // Missing required fields reach MVC validation on the first request without changing any account.
        using var first = await client.PostAsync("/Account/" + action, new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        using var second = await client.PostAsync("/Account/" + action + "/999", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
        Assert.NotNull(second.Headers.RetryAfter);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Account/" + action)).StatusCode);
    }

    private static void AssertChallenge(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location?.OriginalString);
    }
}

public sealed class SessionFixture : IDisposable
{
    public const string Password = "Session-original-password-123";
    private static readonly string Hash = PasswordHashing.HashNewPassword(Password);
    private readonly ManagementApplicationFactory root = new();
    public WebApplicationFactory<Program> Application { get; }
    public SessionFixture()
    {
        var name = Guid.NewGuid().ToString();
        Application = root.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<DmsDbContext>();
            services.AddScoped(_ => new DmsDbContext(new DbContextOptionsBuilder<DmsDbContext>().UseInMemoryDatabase(name).Options));
            services.RemoveAll<IAccountSessionValidator>();
            services.AddScoped<IAccountSessionValidator, AccountSessionValidator>();
        }));
    }
    public void Reset()
    {
        using var scope = Application.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DmsDbContext>(); db.Database.EnsureDeleted();
        db.Roles.AddRange(new Role { RoleId = 1, RoleName = "SuperAdmin" }, new Role { RoleId = 2, RoleName = "Dispatcher" });
        db.Merchants.Add(new Merchant { MerchantId = 1, ShopName = "Session shop", Email = "merchant@example.invalid", Password = Hash });
        db.Employees.AddRange(new Employee { EmployeeId = 1, RoleId = 1, Name = "Administrator", Email = "admin@example.invalid", Password = Hash },
            new Employee { EmployeeId = 2, Name = "Driver", Email = "driver@example.invalid", Password = Hash });
        db.Drivers.Add(new Driver { DriverId = 1, EmployeeId = 2, Name = "Driver" });
        db.SaveChanges();
    }
    public async Task<List<Claim>> Claims(string role, string password = Password)
    {
        using var scope = Application.Services.CreateScope();
        var claims = await scope.ServiceProvider.GetRequiredService<IAccountService>().AuthenticateUserAsync(new LoginViewModel
        {
            Email = role switch { "Merchant" => "merchant@example.invalid", "Driver" => "driver@example.invalid", _ => "admin@example.invalid" }, Password = password
        });
        Assert.NotNull(claims); return claims;
    }
    public async Task<string> Token(string role, string password = Password) => Encode(await Claims(role, password));
    public string Encode(IEnumerable<Claim> claims)
    {
        var settings = Application.Services.GetRequiredService<JwtSettings>();
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(settings.Issuer, settings.Audience, claims,
            expires: DateTime.UtcNow.AddMinutes(10), signingCredentials: new SigningCredentials(settings.SigningKey, SecurityAlgorithms.HmacSha256)));
    }
    public HttpClient Client(string? token = null)
    {
        var client = Application.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        if (token != null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
    public async Task<string> Antiforgery(HttpClient client)
    {
        var page = await client.GetStringAsync("/Profile");
        return WebUtility.HtmlDecode(Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    }
    public async Task Mutate(Action<DmsDbContext> update) => await InScope(async services =>
    {
        var db = services.GetRequiredService<DmsDbContext>(); update(db); await db.SaveChangesAsync();
    });
    public async Task InScope(Func<IServiceProvider, Task> action)
    {
        using var scope = Application.Services.CreateScope(); await action(scope.ServiceProvider);
    }
    public void Dispose() { Application.Dispose(); root.Dispose(); }
}
