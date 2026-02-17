using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class EmployeeQryConfiguration : IEntityTypeConfiguration<EmployeeQry>
    {
        public void Configure(EntityTypeBuilder<EmployeeQry> builder)
        {
            builder.ToTable("EmployeeQry");
            builder.HasKey(e => e.Id);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.CompanyID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.Code).HasColumnType("nvarchar(8)").HasMaxLength(8).IsRequired();
            builder.Property(e => e.Name).HasColumnType("nvarchar(32)").HasMaxLength(32).IsRequired();
            builder.Property(e => e.FixedInvisible).HasColumnType("bit").IsRequired();
            builder.Property(e => e.ExceptIDList).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.IDList).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.InvisibleID).HasColumnType("decimal(1, 0)");
            builder.Property(e => e.FunctionID).HasColumnType("decimal(6, 0)");
        }
    }
}
