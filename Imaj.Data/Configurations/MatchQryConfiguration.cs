using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class MatchQryConfiguration : IEntityTypeConfiguration<MatchQry>
    {
        public void Configure(EntityTypeBuilder<MatchQry> builder)
        {
            builder.ToTable("MatchQry");
            builder.HasKey(e => e.Id);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.FunctionID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.AtomicDT1).HasColumnType("smalldatetime").IsRequired();
            builder.Property(e => e.AtomicDT2).HasColumnType("smalldatetime").IsRequired();
            builder.Property(e => e.ResourceIDList).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.ExceptReserveIDList).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
        }
    }
}
