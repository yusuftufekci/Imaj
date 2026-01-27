using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class EmpTimeConfiguration : IEntityTypeConfiguration<EmpTime>
    {
        public void Configure(EntityTypeBuilder<EmpTime> builder)
        {
            builder.ToTable("EmpTime");
            
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(18, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.EmployeeID).HasColumnType("decimal(18, 0)").IsRequired();
            builder.Property(e => e.TimeTypeID).HasColumnType("decimal(18, 0)").IsRequired();
            builder.Property(e => e.Default).IsRequired();
            builder.Property(e => e.Deleted).HasColumnType("decimal(18, 0)").IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            // Relationships
            builder.HasOne(d => d.Employee)
                .WithMany(p => p.EmpTimes)
                .HasForeignKey(d => d.EmployeeID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.TimeType)
                .WithMany(p => p.EmpTimes)
                .HasForeignKey(d => d.TimeTypeID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
