using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Imaj.Web.Services.Reports;

namespace Imaj.Web.Extensions
{
    /// <summary>
    /// Web katmanı için DI extension metodları.
    /// Authentication, Localization ve diğer web servislerini içerir.
    /// </summary>
    public static class WebServiceExtensions
    {
        /// <summary>
        /// Web katmanı servislerini DI container'a ekler.
        /// Authentication, Cookie ayarları vb.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection (fluent interface için)</returns>
        public static IServiceCollection AddWebServices(this IServiceCollection services)
        {
            // Cookie Authentication
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Auth/Login";
                    options.LogoutPath = "/Auth/Logout";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                });

            services.AddScoped<IOvertimeReportExcelService, OvertimeReportExcelService>();

            return services;
        }

        /// <summary>
        /// Localization servislerini DI container'a ekler.
        /// Çoklu dil desteği için kullanılır.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection (fluent interface için)</returns>
        public static IServiceCollection AddLocalizationServices(this IServiceCollection services)
        {
            services.AddLocalization(options => options.ResourcesPath = "Resources");
            
            services.AddControllersWithViews()
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization();

            return services;
        }
    }
}
