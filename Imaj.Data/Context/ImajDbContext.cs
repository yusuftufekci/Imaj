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
        public DbSet<JobWork> JobWorks { get; set; }
        public DbSet<Reserve> Reserves { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<Allocate> Allocates { get; set; }
        
        // Batch 4 (X Tables & References)
        public DbSet<XWorkType> XWorkTypes { get; set; }
        public DbSet<XFunction> XFunctions { get; set; }
        public DbSet<XProduct> XProducts { get; set; }
        public DbSet<XInterval> XIntervals { get; set; }
        public DbSet<XProdGrp> XProdGrps { get; set; }
        public DbSet<XProdCat> XProdCats { get; set; }
        public DbSet<XReason> XReasons { get; set; }
        public DbSet<XReasonCat> XReasonCats { get; set; }
        public DbSet<XResoCat> XResoCats { get; set; }
        public DbSet<XResource> XResources { get; set; }
        public DbSet<XSort> XSorts { get; set; }
        public DbSet<Sort> Sorts { get; set; }
        public DbSet<XState> XStates { get; set; }
        public DbSet<XTaxType> XTaxTypes { get; set; }
        public DbSet<XTimeType> XTimeTypes { get; set; }
        public DbSet<XTrans> XTranses { get; set; }
        public DbSet<Trans> Transes { get; set; }
        public DbSet<TransCat> TransCats { get; set; }
        public DbSet<TransType> TransTypes { get; set; }
        public DbSet<XTriplet> XTriplets { get; set; }
        public DbSet<Triplet> Triplets { get; set; }
        public DbSet<XLogAction> XLogActions { get; set; }
        public DbSet<LogAction> LogActions { get; set; }

        // Batch 5 & 6 (User, Role, Func, Invo Details, Logs)
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<UserEmp> UserEmps { get; set; }
        public DbSet<UserFunc> UserFuncs { get; set; }
        
        public DbSet<RoleCont> RoleConts { get; set; }
        public DbSet<BaseCont> BaseConts { get; set; }
        public DbSet<RoleIntf> RoleIntfs { get; set; }
        public DbSet<BaseIntf> BaseIntfs { get; set; }
        public DbSet<RoleMenu> RoleMenus { get; set; }
        public DbSet<BaseMenu> BaseMenus { get; set; }
        public DbSet<RoleMeth> RoleMeths { get; set; }
        public DbSet<BaseMeth> BaseMeths { get; set; }
        public DbSet<RoleProp> RoleProps { get; set; }
        public DbSet<BaseProp> BaseProps { get; set; }

        public DbSet<FuncRule> FuncRules { get; set; }
        public DbSet<FuncProd> FuncProds { get; set; }
        public DbSet<FuncReso> FuncResos { get; set; }

        public DbSet<InvoJob> InvoJobs { get; set; }
        public DbSet<InvoProdCat> InvoProdCats { get; set; }
        public DbSet<CustProdCat> CustProdCats { get; set; }
        public DbSet<InvoTax> InvoTaxes { get; set; }

        public DbSet<JobLog> JobLogs { get; set; }
        public DbSet<InvoiceLog> InvoiceLogs { get; set; }
        public DbSet<ReserveLog> ReserveLogs { get; set; }
        public DbSet<MsgLog> MsgLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Tüm entity konfigürasyonlarını bu assembly'den otomatik yükle
            // (Configurations klasöründeki IEntityTypeConfiguration implementasyonları)
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ImajDbContext).Assembly);

            // Explicit Mapping (Garanti Çözüm)
            modelBuilder.Entity<CustProdCat>().ToTable("CustProdCat");
        }
    }
}

