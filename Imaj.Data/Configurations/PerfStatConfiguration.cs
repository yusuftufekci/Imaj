using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class PerfStatConfiguration : IEntityTypeConfiguration<PerfStat>
    {
        public void Configure(EntityTypeBuilder<PerfStat> builder)
        {
            builder.ToTable("PerfStat");

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.StartDT).HasColumnType("smalldatetime").IsRequired();
            builder.Property(e => e.Controller).HasColumnType("nvarchar(16)").HasMaxLength(16).IsRequired();
            builder.Property(e => e.Interface).HasColumnType("nvarchar(32)").HasMaxLength(32).IsRequired();
            builder.Property(e => e.Method).HasColumnType("nvarchar(64)").HasMaxLength(64).IsRequired();
            builder.Property(e => e.Server).HasColumnType("nvarchar(32)").HasMaxLength(32).IsRequired();
            builder.Property(e => e.Success).HasColumnType("bit").IsRequired();
            builder.Property(e => e.FullDuration).HasColumnType("int").IsRequired();
            builder.Property(e => e.MethodDuration).HasColumnType("int").IsRequired();
            builder.Property(e => e.UserID).HasColumnType("decimal(16, 0)");
        }
    }
}
