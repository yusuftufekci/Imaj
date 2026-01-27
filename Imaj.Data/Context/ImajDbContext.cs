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
        public DbSet<Company> Companies { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Interval> Intervals { get; set; }
        public DbSet<Function> Functions { get; set; }
        public DbSet<TimeType> TimeTypes { get; set; }
        public DbSet<WorkType> WorkTypes { get; set; }
        public DbSet<EmpFunc> EmpFuncs { get; set; }
        public DbSet<EmpTime> EmpTimes { get; set; }
        public DbSet<EmpWork> EmpWorks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Tüm entity konfigürasyonlarını bu assembly'den otomatik yükle
            // (Configurations klasöründeki IEntityTypeConfiguration implementasyonları)
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ImajDbContext).Assembly);
        }
    }
}

