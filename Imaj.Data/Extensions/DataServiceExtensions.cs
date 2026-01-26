using Imaj.Core.Interfaces.Repositories;
using Imaj.Data.Context;
using Imaj.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Imaj.Data.Extensions
{
    /// <summary>
    /// Data katmanı için DI extension metodları.
    /// DbContext, Repository ve UnitOfWork kayıtlarını içerir.
    /// </summary>
    public static class DataServiceExtensions
    {
        /// <summary>
        /// Data katmanı servislerini DI container'a ekler.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Uygulama konfigürasyonu</param>
        /// <returns>Service collection (fluent interface için)</returns>
        public static IServiceCollection AddDataServices(this IServiceCollection services, IConfiguration configuration)
        {
            // DbContext - SQL Server bağlantısı
            services.AddDbContext<ImajDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // Generic Repository
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            
            // Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            
            // Specific Repositories
            services.AddScoped<ICustomerRepository, CustomerRepository>();

            return services;
        }
    }
}
