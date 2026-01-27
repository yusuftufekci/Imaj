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
        
        // Batch 1
        public DbSet<Language> Languages { get; set; }
        public DbSet<Culture> Cultures { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<ReasonCat> ReasonCats { get; set; }
        public DbSet<Reason> Reasons { get; set; }
        public DbSet<TaxType> TaxTypes { get; set; }
        public DbSet<ProdGrp> ProdGrps { get; set; }
        public DbSet<ProdCat> ProdCats { get; set; }
        
        // Batch 2 & 3
        public DbSet<ResoCat> ResoCats { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoLine> InvoLines { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<JobProd> JobProds { get; set; }
        public DbSet<Reserve> Reserves { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<Allocate> Allocates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Tüm entity konfigürasyonlarını bu assembly'den otomatik yükle
            // (Configurations klasöründeki IEntityTypeConfiguration implementasyonları)
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ImajDbContext).Assembly);
        }
    }
}

