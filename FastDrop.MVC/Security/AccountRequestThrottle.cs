using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;

namespace FastDrop.MVC.Security;

public class AccountThrottleOptions
{
    public int LoginIpLimit { get; set; } = 20;
    public int LoginAccountLimit { get; set; } = 5;
    public int SignupIpLimit { get; set; } = 3;
    public int SignupAccountLimit { get; set; } = 3;
}

public sealed class AccountRequestThrottle : IDisposable
{
    private readonly PartitionedRateLimiter<string> sources;
    private readonly PartitionedRateLimiter<string> accounts;

    public AccountRequestThrottle(IOptions<AccountThrottleOptions> options)
    {
        var limits = options.Value;
        if (new[] { limits.LoginIpLimit, limits.LoginAccountLimit, limits.SignupIpLimit, limits.SignupAccountLimit }.Any(n => n < 1))
            throw new InvalidOperationException("Account request limits must be positive.");
        sources = Build(limits.LoginIpLimit, limits.SignupIpLimit);
        accounts = Build(limits.LoginAccountLimit, limits.SignupAccountLimit);
    }

    public static bool Applies(HttpContext context)
    {
        var action = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
        return HttpMethods.IsPost(context.Request.Method) && action?.ControllerName == "Account" && action.ActionName is "Login" or "Signup";
    }

    public async Task<bool> AllowAsync(HttpContext context, IAccountThrottleKeyResolver? resolver = null)
    {
        if (!Applies(context)) return true;
        var action = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
        var prefix = action!.ActionName == "Signup" ? "signup:" : "login:";
        // Read the connection IP directly; arbitrary forwarding headers are not trusted here.
        using var source = await sources.AcquireAsync(prefix + (context.Connection.RemoteIpAddress?.ToString() ?? "unknown"), 1, context.RequestAborted);
        if (!source.IsAcquired) return await RejectAsync(context, source);
        if (!context.Request.HasFormContentType)
        {
            context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
            await context.Response.WriteAsync("Use the account form to submit this request.", context.RequestAborted);
            return false;
        }
        IFormCollection form;
        try { form = await context.Request.ReadFormAsync(context.RequestAborted); }
        catch (InvalidDataException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("The account form could not be read.", context.RequestAborted);
            return false;
        }
        var email = form["Email"].ToString().Trim();
        // Bound account keys; oversized/duplicate fields are invalid input, not distinct limiter partitions.
        var key = email.Length is < 1 or > 254 || form["Email"].Count != 1 ? "invalid"
            : resolver == null ? "email:" + email.ToUpperInvariant() : await resolver.ResolveAsync(email, context.RequestAborted);
        using var account = await accounts.AcquireAsync(prefix + key, 1, context.RequestAborted);
        return account.IsAcquired || await RejectAsync(context, account);
    }

    private static PartitionedRateLimiter<string> Build(int login, int signup) => PartitionedRateLimiter.Create<string, string>(key =>
        RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = key.StartsWith("signup:", StringComparison.Ordinal) ? signup : login,
            Window = TimeSpan.FromMinutes(key.StartsWith("signup:", StringComparison.Ordinal) ? 15 : 1),
            QueueLimit = 0, AutoReplenishment = true
        }));

    private static async Task<bool> RejectAsync(HttpContext context, RateLimitLease lease)
    {
        var seconds = lease.TryGetMetadata(MetadataName.RetryAfter, out var retry) ? Math.Max(1, (int)Math.Ceiling(retry.TotalSeconds)) : 60;
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
        context.Response.Headers.CacheControl = "no-store";
        await context.Response.WriteAsync("Too many account requests. Please try again later.", context.RequestAborted);
        return false;
    }
    public void Dispose() { sources.Dispose(); accounts.Dispose(); }
}
