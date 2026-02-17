using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class ResourceQryConfiguration : IEntityTypeConfiguration<ResourceQry>
    {
        public void Configure(EntityTypeBuilder<ResourceQry> builder)
        {
            builder.ToTable("ResourceQry");
            builder.HasKey(e => e.Id);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(8, 0)").IsRequired();
            builder.Property(e => e.CompanyID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.OwnUserID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.Code).HasColumnType("nvarchar(8)").HasMaxLength(8).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.ExceptIDList).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.ResoCatIDList).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.FunctionSecurity).HasColumnType("bit").IsRequired();
            builder.Property(e => e.Sequence1).HasColumnType("int");
            builder.Property(e => e.Sequence2).HasColumnType("int");
            builder.Property(e => e.FunctionID).HasColumnType("decimal(6, 0)");
            builder.Property(e => e.InvisibleID).HasColumnType("decimal(1, 0)");
            builder.Property(e => e.ResoCatID).HasColumnType("decimal(6, 0)");
        }
    }
}
