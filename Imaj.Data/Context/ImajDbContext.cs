using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Imaj.Data.Context
{
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
            
            // Legacy Table Mapping
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users"); // Adjust this to match legacy table name
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).HasMaxLength(100);
            });

            // Customer Mapping
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("Customer");
                entity.HasKey(e => e.Id);
                // Ignore BaseEntity properties not in legacy schema
                entity.Ignore(e => e.CreatedDate);
                entity.Ignore(e => e.IsActive);

                // Handle Decimal for ID and CompanyID
                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("decimal(18, 0)"); // Explicitly define store type
                    
                entity.Property(e => e.CompanyID)
                    .HasColumnType("decimal(18, 0)");

                entity.Property(e => e.Code).HasMaxLength(8);
                entity.Property(e => e.Name).HasMaxLength(32);
                entity.Property(e => e.City).HasMaxLength(32);
                entity.Property(e => e.Phone).HasMaxLength(32);
                entity.Property(e => e.Fax).HasMaxLength(32);
                entity.Property(e => e.EMail).HasMaxLength(64);
                entity.Property(e => e.InvoName).HasMaxLength(64);
                entity.Property(e => e.Contact).HasMaxLength(32);
                entity.Property(e => e.TaxOffice).HasMaxLength(32);
                entity.Property(e => e.TaxNumber).HasMaxLength(32);
                entity.Property(e => e.Country).HasMaxLength(32);
                entity.Property(e => e.Owner).HasMaxLength(32);
                entity.Property(e => e.Zip).HasMaxLength(32);
                
                // NText usually doesn't need MaxLength in EF Core unless converted to nvarchar(max)
                // entity.Property(e => e.Address).HasColumnType("ntext");
                // entity.Property(e => e.Notes).HasColumnType("ntext");
            });
        }
    }
}
