using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class BaseMethQryConfiguration : IEntityTypeConfiguration<BaseMethQry>
    {
        public void Configure(EntityTypeBuilder<BaseMethQry> builder)
        {
            builder.ToTable("BaseMethQry");
            builder.HasKey(e => e.Id);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.ExceptIDList).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.BaseContID).HasColumnType("decimal(6, 0)");
        }
    }
}
