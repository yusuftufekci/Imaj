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
        }
    }
}
