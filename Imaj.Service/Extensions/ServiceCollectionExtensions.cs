using FluentValidation;
using Imaj.Service.Interfaces;
using Imaj.Service.Mapping;
using Imaj.Service.Options;
using Imaj.Service.Services;
using Imaj.Service.Validators;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Imaj.Service.Extensions
{
    /// <summary>
    /// Service katmanı için DI extension metodları.
    /// Options, Services, AutoMapper ve FluentValidation kayıtlarını içerir.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Service katmanı servislerini DI container'a ekler.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Uygulama konfigürasyonu</param>
        /// <returns>Service collection (fluent interface için)</returns>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Options Pattern - Configuration binding
            services.Configure<CustomerSettings>(configuration.GetSection(CustomerSettings.SectionName));
            services.Configure<AuthSettings>(configuration.GetSection(AuthSettings.SectionName));
            services.Configure<SecurityHeadersSettings>(configuration.GetSection(SecurityHeadersSettings.SectionName));
            services.Configure<DeploymentSettings>(configuration.GetSection(DeploymentSettings.SectionName));
            services.AddMemoryCache();
            services.AddHttpContextAccessor();

            // Business Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<ICurrentPermissionContext, CurrentPermissionContext>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<IInvoiceService, InvoiceService>();
            services.AddScoped<IJobService, JobService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IProductPageService, ProductPageService>();
            services.AddScoped<IProductReportService, ProductReportService>();
            services.AddScoped<IResoCatService, ResoCatService>();
            services.AddScoped<IFunctionService, FunctionService>();
            services.AddScoped<IResourceService, ResourceService>();
            services.AddScoped<IReasonService, ReasonService>();
            services.AddScoped<IWorkTypeService, WorkTypeService>();
            services.AddScoped<ITimeTypeService, TimeTypeService>();
            services.AddScoped<ITaxTypeService, TaxTypeService>();
            services.AddScoped<IProdCatService, ProdCatService>();
            services.AddScoped<IProdGrpService, ProdGrpService>();
            services.AddScoped<IAbsenceService, AbsenceService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ILookupService, LookupService>();

            // AutoMapper - Assembly scan ile profil bulma
            services.AddAutoMapper(typeof(MappingProfile).Assembly);

            // FluentValidation - Assembly scan ile validator bulma
            services.AddValidatorsFromAssemblyContaining<CustomerDtoValidator>();

            return services;
        }
    }
}
