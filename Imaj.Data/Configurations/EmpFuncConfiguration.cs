using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class EmpFuncConfiguration : IEntityTypeConfiguration<EmpFunc>
    {
        public void Configure(EntityTypeBuilder<EmpFunc> builder)
        {
            builder.ToTable("EmpFunc");
            
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(18, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.EmployeeID).HasColumnType("decimal(18, 0)").IsRequired();
            builder.Property(e => e.FunctionID).HasColumnType("decimal(18, 0)").IsRequired();
            builder.Property(e => e.WorkAmountUpdate).IsRequired();
            builder.Property(e => e.Deleted).HasColumnType("decimal(18, 0)").IsRequired(); // Using 18,0 for safety
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            // Relationships
            builder.HasOne(d => d.Employee)
                .WithMany(p => p.EmpFuncs)
                .HasForeignKey(d => d.EmployeeID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.Function)
                .WithMany(p => p.EmpFuncs)
                .HasForeignKey(d => d.FunctionID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
