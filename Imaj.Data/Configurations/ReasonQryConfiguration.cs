using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class ReasonQryConfiguration : IEntityTypeConfiguration<ReasonQry>
    {
        public void Configure(EntityTypeBuilder<ReasonQry> builder)
        {
            builder.ToTable("ReasonQry");
            builder.HasKey(e => e.Id);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.CompanyID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.Code).HasColumnType("nvarchar(8)").HasMaxLength(8).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.ReasonCatID).HasColumnType("decimal(2, 0)");
            builder.Property(e => e.InvisibleID).HasColumnType("decimal(1, 0)");
            builder.Property(e => e.SortLangID).HasColumnType("decimal(2, 0)");
        }
    }
}
