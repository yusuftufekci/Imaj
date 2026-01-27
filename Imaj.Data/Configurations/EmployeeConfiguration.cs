using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
    {
        public void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.ToTable("Employee");
            
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(18, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.CompanyID).HasColumnType("decimal(18, 0)").IsRequired();
            builder.Property(e => e.Code).HasMaxLength(8).IsRequired();
            builder.Property(e => e.Name).HasMaxLength(32).IsRequired();
            builder.Property(e => e.SelectQty).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.Invisible).IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();

            // Relationships
            builder.HasOne(d => d.Company)
                .WithMany(p => p.Employees)
                .HasForeignKey(d => d.CompanyID)
                .OnDelete(DeleteBehavior.NoAction); // Usually legacy DBs have logic or constraints preventing cascade
        }
    }
}
