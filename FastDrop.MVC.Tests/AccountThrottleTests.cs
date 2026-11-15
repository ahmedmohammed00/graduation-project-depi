using System.Net;
using FastDrop.MVC.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace FastDrop.MVC.Tests;

public class AccountThrottleTests
{
    [Fact]
    public async Task LoginBudgetsApplyToBothSourceAndNormalizedAccountAcrossDifferentSources()
    {
        using var throttle = NewThrottle();
        Assert.True(await throttle.AllowAsync(Request("Login", "192.0.2.1", "User@example.invalid")));
        var sameAccount = Request("Login", "192.0.2.2", " user@EXAMPLE.invalid ");
        Assert.False(await throttle.AllowAsync(sameAccount));
        Assert.Equal(429, sameAccount.Response.StatusCode);
        Assert.True(int.Parse(sameAccount.Response.Headers.RetryAfter.ToString()) > 0);
        Assert.Equal("no-store", sameAccount.Response.Headers.CacheControl);
        Assert.True(await throttle.AllowAsync(Request("Login", "192.0.2.1", "another@example.invalid")));
        var sameSource = Request("Login", "192.0.2.1", "third@example.invalid");
        Assert.False(await throttle.AllowAsync(sameSource));
        Assert.Equal(429, sameSource.Response.StatusCode);
    }

    [Fact]
    public async Task SpoofedForwardingHeadersDoNotChangeTheRateLimitSource()
    {
        using var throttle = NewThrottle();
        for (var i = 0; i < 2; i++)
        {
            var request = Request("Login", "192.0.2.1", $"{i}@example.invalid");
            request.Request.Headers["X-Forwarded-For"] = "203.0.113." + i;
            Assert.True(await throttle.AllowAsync(request));
        }
        var limited = Request("Login", "192.0.2.1", "last@example.invalid");
        limited.Request.Headers["X-Forwarded-For"] = "203.0.113.99";
        Assert.False(await throttle.AllowAsync(limited));
    }

    [Fact]
    public async Task SignupHasItsOwnSourceBudgetAndLoginReadsRemainAvailable()
    {
        using var throttle = NewThrottle();
        Assert.True(await throttle.AllowAsync(Request("Signup", "192.0.2.1", "one@example.invalid")));
        Assert.False(await throttle.AllowAsync(Request("Signup", "192.0.2.1", "two@example.invalid")));
        Assert.True(await throttle.AllowAsync(Request("Login", "192.0.2.1", "one@example.invalid")));
        var read = Request("Signup", "192.0.2.1", "one@example.invalid"); read.Request.Method = "GET";
        Assert.True(await throttle.AllowAsync(read));
        var unrelated = Request("ChangePassword", "192.0.2.1", "one@example.invalid");
        Assert.True(await throttle.AllowAsync(unrelated));
    }

    [Fact]
    public async Task MalformedAndDuplicateAccountFieldsCannotCreateDistinctAccountPartitions()
    {
        using var throttle = NewThrottle();
        var first = Request("Login", "192.0.2.1", new string('a', 300));
        Assert.True(await throttle.AllowAsync(first));
        var second = Request("Login", "192.0.2.2", "");
        second.Request.Form = new FormCollection(new Dictionary<string, StringValues> { ["Email"] = new[] { "a@example.invalid", "b@example.invalid" } });
        Assert.False(await throttle.AllowAsync(second));
    }

    [Fact]
    public async Task JsonAndQueryOnlySubmissionsCannotSkipAccountFormRateLimiting()
    {
        using var throttle = NewThrottle();
        var request = Request("Login", "192.0.2.1", "user@example.invalid");
        request.Request.ContentType = "application/json";
        request.Features.Set<IFormFeature>(new FormFeature(request.Request));
        request.Request.QueryString = new QueryString("?Email=user@example.invalid&Password=guess");
        Assert.False(await throttle.AllowAsync(request));
        Assert.Equal(415, request.Response.StatusCode);
    }

    private static AccountRequestThrottle NewThrottle() => new(Options.Create(new AccountThrottleOptions
    {
        LoginIpLimit = 2, LoginAccountLimit = 1, SignupIpLimit = 1, SignupAccountLimit = 1
    }));

    private static DefaultHttpContext Request(string action, string ip, string email)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        context.Request.Method = "POST"; context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Form = new FormCollection(new Dictionary<string, StringValues> { ["Email"] = email });
        context.Response.Body = new MemoryStream();
        context.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(new ControllerActionDescriptor
        {
            ControllerName = "Account", ActionName = action
        }), "throttle-test"));
        return context;
    }
}
