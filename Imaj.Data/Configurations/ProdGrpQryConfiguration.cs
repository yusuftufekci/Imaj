using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class ProdGrpQryConfiguration : IEntityTypeConfiguration<ProdGrpQry>
    {
        public void Configure(EntityTypeBuilder<ProdGrpQry> builder)
        {
            builder.ToTable("ProdGrpQry");
            builder.HasKey(e => e.Id);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.CompanyID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.ExceptIDList).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.SortLangID).HasColumnType("decimal(2, 0)");
            builder.Property(e => e.InvisibleID).HasColumnType("decimal(1, 0)");
        }
    }
}
