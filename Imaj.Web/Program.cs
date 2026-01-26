using Imaj.Data.Extensions;
using Imaj.Service.Extensions;
using Imaj.Web.Extensions;
using Imaj.Web.Middlewares;
using Microsoft.AspNetCore.Localization;
using Serilog;
using System.Globalization;

// Serilog bootstrap logger (uygulama başlamadan önce)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Uygulama başlatılıyor...");
    
    var builder = WebApplication.CreateBuilder(args);

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
    builder.Services.AddWebServices();

    var app = builder.Build();

    // Exception middleware
    app.UseMiddleware<ExceptionMiddleware>();

    // HTTP pipeline konfigürasyonu
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

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

    app.UseAuthentication();
    app.UseAuthorization();

    // Serilog request logging
    app.UseSerilogRequestLogging();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

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
