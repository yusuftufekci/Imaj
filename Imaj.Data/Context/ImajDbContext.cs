using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Imaj.Data.Context
{
    /// <summary>
    /// Uygulama veritabanı context'i.
    /// Entity konfigürasyonları Configurations klasöründen otomatik yüklenir.
    /// </summary>
    public class ImajDbContext : DbContext
    {
        public ImajDbContext(DbContextOptions<ImajDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Tüm entity konfigürasyonlarını bu assembly'den otomatik yükle
            // (Configurations klasöründeki IEntityTypeConfiguration implementasyonları)
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ImajDbContext).Assembly);
        }
    }
}

