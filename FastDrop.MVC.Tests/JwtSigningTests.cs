using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FastDrop.MVC.Controllers;
using FastDrop.MVC.Security;
using FastDrop.MVC.Services;
using FastDrop.MVC.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace FastDrop.MVC.Tests;

public class JwtSigningTests
{
    // Public test fixture only: this key was exposed before the fix.
    private const string PublishedKey = "ThisIsAVerySecretKeyThatIsAtLeast32CharactersLong!";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-base64!")]
    [InlineData(PublishedKey)]
    public void MissingOrMalformedKeyIsRejected(string? key)
    {
        Assert.Throws<InvalidOperationException>(() => JwtSettings.FromConfiguration(Configuration(key)));
    }

    [Fact]
    public void ShortDecodedKeyIsRejected()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(31));
        Assert.Throws<InvalidOperationException>(() => JwtSettings.FromConfiguration(Configuration(key)));
    }

    [Fact]
    public void EncodingPublishedKeyDoesNotMakeItAcceptable()
    {
        var key = Convert.ToBase64String(Encoding.UTF8.GetBytes(PublishedKey));
        Assert.Throws<InvalidOperationException>(() => JwtSettings.FromConfiguration(Configuration(key)));
    }

    [Theory]
    [InlineData(null, "FastDrop.Tests")]
    [InlineData("FastDrop.Tests", null)]
    [InlineData(" ", "FastDrop.Tests")]
    public void MissingIssuerOrAudienceIsRejected(string? issuer, string? audience)
    {
        var config = Configuration(NewPrivateKey(), issuer, audience);
        Assert.Throws<InvalidOperationException>(() => JwtSettings.FromConfiguration(config));
    }

    [Fact]
    public void ConfigurationErrorDoesNotExposeTheSubmittedKey()
    {
        const string malformedSecret = "private-configuration-must-not-be-logged!";
        var error = Assert.Throws<InvalidOperationException>(
            () => JwtSettings.FromConfiguration(Configuration(malformedSecret)));
        Assert.DoesNotContain(malformedSecret, error.ToString());
    }

    [Fact]
    public async Task LoginTokenValidatesUsingTheConfiguredPrivateKey()
    {
        var settings = JwtSettings.FromConfiguration(Configuration(NewPrivateKey()));
        var controller = new AccountController(new SuccessfulAccountService(), settings)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Login(new LoginViewModel
        {
            Email = "audit@example.invalid", Password = "Test-only-password"
        });

        Assert.IsType<RedirectToActionResult>(result);
        var cookie = controller.Response.Headers.SetCookie.Single(c => c!.StartsWith("jwt="))!;
        var encodedToken = cookie.Split(';', 2)[0]["jwt=".Length..];
        var principal = new JwtSecurityTokenHandler().ValidateToken(
            encodedToken, ValidationParameters(settings), out _);
        Assert.Equal("audit@example.invalid", principal.FindFirst(ClaimTypes.Email)?.Value);
    }

    [Fact]
    public void TokenSignedWithPublishedKeyIsRejectedAfterRotation()
    {
        var settings = JwtSettings.FromConfiguration(Configuration(NewPrivateKey()));
        var forgedToken = new JwtSecurityToken(settings.Issuer, settings.Audience,
            new[] { new Claim(ClaimTypes.Role, "SuperAdmin") },
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(PublishedKey)), SecurityAlgorithms.HmacSha256));
        var handler = new JwtSecurityTokenHandler();

        Assert.ThrowsAny<SecurityTokenException>(() => handler.ValidateToken(
            handler.WriteToken(forgedToken), ValidationParameters(settings), out _));
    }

    private static string NewPrivateKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static IConfiguration Configuration(string? key, string? issuer = "FastDrop.Tests",
        string? audience = "FastDrop.Tests") => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:Key"] = key,
            ["JwtSettings:Issuer"] = issuer,
            ["JwtSettings:Audience"] = audience
        }).Build();

    private static TokenValidationParameters ValidationParameters(JwtSettings settings) => new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = settings.Issuer,
        ValidAudience = settings.Audience,
        IssuerSigningKey = settings.SigningKey
    };

    private sealed class SuccessfulAccountService : IAccountService
    {
        public Task<bool> EmailExistsAsync(string email) => throw new NotSupportedException();
        public Task CreateUserAsync(SignupViewModel model) => throw new NotSupportedException();
        public Task<List<Claim>?> AuthenticateUserAsync(LoginViewModel model) => Task.FromResult<List<Claim>?>(
            new List<Claim> { new(ClaimTypes.Email, model.Email), new(ClaimTypes.Role, "Merchant") });
    }
}
