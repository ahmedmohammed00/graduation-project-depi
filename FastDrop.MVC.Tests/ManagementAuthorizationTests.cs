using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using FastDrop.MVC.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace FastDrop.MVC.Tests;

public class ManagementAuthorizationTests : IClassFixture<ManagementApplicationFactory>
{
    private readonly ManagementApplicationFactory _application;

    public ManagementAuthorizationTests(ManagementApplicationFactory application) => _application = application;

    public static TheoryData<string, string[]> ManagementResources => new()
    {
        { "Drivers", new[] { "SuperAdmin" } },
        { "DriverPayments", new[] { "SuperAdmin" } },
        { "Products", new[] { "SuperAdmin" } },
        { "Zones", new[] { "SuperAdmin" } },
        { "Routes", new[] { "SuperAdmin", "Dispatcher" } },
        { "Performance", new[] { "SuperAdmin", "Dispatcher" } }
    };

    public static IEnumerable<object[]> AnonymousManagementResources =>
        ManagementResources.Select(resource => new[] { resource[0] });

    [Theory]
    [MemberData(nameof(AnonymousManagementResources))]
    public async Task AnonymousManagementReadsAndWritesRequireLogin(string controller)
    {
        using var client = CreateClient();
        using var read = await client.GetAsync($"/{controller}/Index");
        AssertLoginChallenge(read);
        using var write = await client.PostAsync(MutationPath(controller), new FormUrlEncodedContent(new Dictionary<string, string>()));
        AssertLoginChallenge(write);
    }

    [Theory]
    [MemberData(nameof(ManagementResources))]
    public async Task OtherRolesCannotReadOrMutateManagementResources(string controller, string[] allowedRoles)
    {
        var deniedRoles = new[] { "Merchant", "Driver", "Employee", "Dispatcher", "Unknown" }.Except(allowedRoles);
        foreach (var role in deniedRoles)
        {
            using var client = CreateClient(role);
            using var read = await client.GetAsync($"/{controller}/Index");
            Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);
            using var write = await client.PostAsync(MutationPath(controller), new FormUrlEncodedContent(new Dictionary<string, string>()));
            Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
        }
    }

    [Theory]
    [MemberData(nameof(ManagementResources))]
    public async Task AuthorizedRolesReachTheActionAndStillNeedAntiforgeryForWrites(string controller, string[] allowedRoles)
    {
        foreach (var role in allowedRoles)
        {
            using var client = CreateClient(role);
            // The action returns 404 for an omitted id before querying the database.
            var action = controller == "Performance" ? "Driver" : "Details";
            using var read = await client.GetAsync($"/{controller}/{action}");
            Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
            using var write = await client.PostAsync(MutationPath(controller), new FormUrlEncodedContent(new Dictionary<string, string>()));
            Assert.Equal(HttpStatusCode.BadRequest, write.StatusCode);
        }
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Home/Privacy")]
    [InlineData("/Home/Error")]
    [InlineData("/Account/Login")]
    [InlineData("/Account/Signup")]
    [InlineData("/Account/AccessDenied")]
    [InlineData("/css/site.css")]
    [InlineData("/lib/bootstrap/dist/css/bootstrap.min.css")]
    [InlineData("/lib/jquery/dist/jquery.min.js")]
    public async Task PublicPagesAndAssetsRemainAccessible(string path)
    {
        using var client = CreateClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/Account/Login")]
    [InlineData("/Account/Signup")]
    public async Task PublicAccountPostsRequireAntiforgeryInsteadOfAuthentication(string path)
    {
        using var client = CreateClient();
        using var response = await client.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>()));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FallbackProtectsEndpointsWithoutExplicitAuthorizationMetadata()
    {
        using var anonymousClient = CreateClient();
        using var anonymousResponse = await anonymousClient.GetAsync("/authorization-test/protected");
        AssertLoginChallenge(anonymousResponse);

        using var authenticatedClient = CreateClient("Merchant");
        using var authenticatedResponse = await authenticatedClient.GetAsync("/authorization-test/protected");
        Assert.Equal(HttpStatusCode.OK, authenticatedResponse.StatusCode);
    }

    private HttpClient CreateClient(string? role = null)
    {
        var client = _application.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = false
        });
        if (role != null)
        {
            var settings = _application.Services.GetRequiredService<JwtSettings>();
            var token = new JwtSecurityToken(settings.Issuer, settings.Audience,
                new[] { new Claim(ClaimTypes.Role, role) }, expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new SigningCredentials(settings.SigningKey, SecurityAlgorithms.HmacSha256));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        }
        return client;
    }

    private static string MutationPath(string controller) => controller == "Performance"
        ? "/Performance/Calculate" : $"/{controller}/Delete/1";

    private static void AssertLoginChallenge(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location?.OriginalString);
    }
}

public sealed class ManagementApplicationFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["JwtSettings:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                ["JwtSettings:Issuer"] = "FastDrop.AuthorizationTests",
                ["JwtSettings:Audience"] = "FastDrop.AuthorizationTests"
            }));
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            // Keep antiforgery tests isolated from the developer's persisted keys.
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            // Existing policy/resource tests use synthetic identities independently of account sessions.
            // SessionSecurityTests explicitly restore the production validator and exercise real tokens/accounts.
            services.RemoveAll<IAccountSessionValidator>();
            services.AddSingleton<IAccountSessionValidator, SyntheticPolicyTestSessions>();
            services.PostConfigure<AccountThrottleOptions>(options =>
            {
                options.LoginIpLimit = options.LoginAccountLimit = options.SignupIpLimit = options.SignupAccountLimit = 10000;
            });
            services.AddControllers().AddApplicationPart(typeof(AuthorizationFallbackProbeController).Assembly);
        });
    }
}

internal sealed class SyntheticPolicyTestSessions : IAccountSessionValidator
{
    public Task<bool> ValidateAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task RevokeAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

// Only registered by the test factory; intentionally has no authorization attribute.
[Route("authorization-test/protected")]
public sealed class AuthorizationFallbackProbeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok();
}
