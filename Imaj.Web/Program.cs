using Imaj.Data.Extensions;
using Imaj.Service.Extensions;
using Imaj.Service.Options;
using Imaj.Web.Extensions;
using Imaj.Web.HealthChecks;
using Imaj.Web.Middlewares;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using System.Globalization;
using System.Linq;
using System.Threading.RateLimiting;

// Serilog bootstrap logger (uygulama başlamadan önce)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Uygulama başlatılıyor...");
    
    var builder = WebApplication.CreateBuilder(args);
    var authSettings = builder.Configuration.GetSection(AuthSettings.SectionName).Get<AuthSettings>() ?? new AuthSettings();
    var securityHeadersSettings = builder.Configuration.GetSection(SecurityHeadersSettings.SectionName).Get<SecurityHeadersSettings>() ?? new SecurityHeadersSettings();

    // Serilog - appsettings.json'dan konfigürasyon
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Localization servisleri (AddControllersWithViews burada)
    builder.Services.AddLocalizationServices();

    // Data katmanı servisleri (DbContext, Repositories, UoW)
    builder.Services.AddDataServices(builder.Configuration);

    // Application servisleri (Options, Services, AutoMapper, FluentValidation)
    builder.Services.AddApplicationServices(builder.Configuration);

    // Web servisleri (Authentication)
    builder.Services.AddWebServices(builder.Configuration);

    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
        .AddCheck<DatabaseReadyHealthCheck>("database", tags: new[] { "ready" });

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("login", context =>
        {
            var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authSettings.LoginRateLimitPermitLimit > 0 ? authSettings.LoginRateLimitPermitLimit : 10,
                Window = TimeSpan.FromMinutes(authSettings.LoginRateLimitWindowMinutes > 0 ? authSettings.LoginRateLimitWindowMinutes : 1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
        });
    });

    var app = builder.Build();

    // Exception middleware
    app.UseMiddleware<ExceptionMiddleware>();

    // HTTP pipeline konfigürasyonu
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["X-Frame-Options"] = "DENY";

        if (!string.IsNullOrWhiteSpace(securityHeadersSettings.CspValue))
        {
            var cspHeaderName = securityHeadersSettings.CspReportOnly
                ? "Content-Security-Policy-Report-Only"
                : "Content-Security-Policy";

            context.Response.Headers[cspHeaderName] = securityHeadersSettings.CspValue;
        }

        await next();
    });

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    // Localization middleware
    var supportedCultures = new[] { new CultureInfo("tr-TR"), new CultureInfo("en-US") };
    app.UseRequestLocalization(new RequestLocalizationOptions
    {
        DefaultRequestCulture = new RequestCulture("tr-TR"),
        SupportedCultures = supportedCultures,
        SupportedUICultures = supportedCultures
    });

    app.UseRouting();
    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    // Serilog request logging
    app.UseSerilogRequestLogging();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.MapHealthChecks("/healthz", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("live")
    });

    app.MapHealthChecks("/readyz", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

    Log.Information("Uygulama başlatıldı.");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Uygulama beklenmeyen bir hata ile sonlandı.");
}
finally
{
    Log.CloseAndFlush();
}
