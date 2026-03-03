using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Imaj.Service.Options;
using Imaj.Web.Authorization;
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
        public static IServiceCollection AddWebServices(this IServiceCollection services, IConfiguration configuration)
        {
            var authSettings = configuration.GetSection(AuthSettings.SectionName).Get<AuthSettings>() ?? new AuthSettings();
            var sessionTimeout = authSettings.SessionTimeoutMinutes <= 0 ? 45 : authSettings.SessionTimeoutMinutes;

            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            // Cookie Authentication
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Auth/Login";
                    options.LogoutPath = "/Auth/Logout";
                    options.AccessDeniedPath = "/Auth/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(sessionTimeout);
                    options.SlidingExpiration = true;
                });

            services.AddScoped<IPageRouteResolver, PageRouteResolver>();
            services.AddScoped<ImajAuthorizationFilter>();
            services.AddScoped<IPermissionViewService, PermissionViewService>();
            services.AddScoped<IOvertimeReportExcelService, OvertimeReportExcelService>();
            services.AddScoped<IProductReportExcelService, ProductReportExcelService>();
            services.AddScoped<ICustomerReportExcelService, CustomerReportExcelService>();

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
            
            services.AddControllersWithViews(options =>
                {
                    options.Filters.Add(new AuthorizeFilter());
                    options.Filters.AddService<ImajAuthorizationFilter>();
                })
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization();

            return services;
        }
    }
}
