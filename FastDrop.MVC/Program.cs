using FastDrop.MVC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using FastDrop.MVC.Security;
using FastDrop.MVC.Services;
var migrateLegacyPasswords = args.Contains("--migrate-legacy-passwords", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(args.Where(arg => arg != "--migrate-legacy-passwords").ToArray());
var jwtSettings = JwtSettings.FromConfiguration(builder.Configuration);
builder.Services.AddSingleton(jwtSettings);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddDbContext<DmsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPerformanceCalculationService, PerformanceCalculationService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<CredentialMigrationService>();
builder.Services.AddScoped<IShipmentService, ShipmentService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IDriverService, DriverService>();
builder.Services.AddScoped<IDriverPaymentService, DriverPaymentService>();
builder.Services.AddScoped<IMerchantService, MerchantService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IRouteService, RouteService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IZoneService, ZoneService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IDispatcherService, DispatcherService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = jwtSettings.SigningKey
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Try to read the token from a cookie for Razor Pages integration
                if (context.Request.Cookies.ContainsKey("jwt"))
                {
                    context.Token = context.Request.Cookies["jwt"];
                }
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                // Redirect to login if not authenticated for browser requests
                if (!context.Request.Path.StartsWithSegments("/api") && !context.Response.HasStarted)
                {
                    context.Response.Redirect("/Account/Login");
                    context.HandleResponse();
                }
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

if (migrateLegacyPasswords)
{
    await using var scope = app.Services.CreateAsyncScope();
    var result = await scope.ServiceProvider.GetRequiredService<CredentialMigrationService>()
        .MigrateLegacyPasswordsAsync();
    app.Logger.LogInformation(
        "Credential migration completed: {Employees} employees and {Merchants} merchants migrated; {RequiresReset} credentials require a verified password reset.",
        result.Employees, result.Merchants, result.RequiresReset);
    Environment.ExitCode = result.RequiresReset == 0 ? 0 : 2;
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

public partial class Program { }
