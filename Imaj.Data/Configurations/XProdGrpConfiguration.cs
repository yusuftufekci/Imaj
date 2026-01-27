using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class XProdGrpConfiguration : IEntityTypeConfiguration<XProdGrp>
    {
        public void Configure(EntityTypeBuilder<XProdGrp> builder)
        {
            builder.ToTable("XProdGrp");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(8, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.ProdGrpID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.LanguageID).HasColumnType("decimal(2, 0)").IsRequired();
            builder.Property(e => e.Name).HasMaxLength(32).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.ProdGrp)
                .WithMany()
                .HasForeignKey(d => d.ProdGrpID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.Language)
                .WithMany()
                .HasForeignKey(d => d.LanguageID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
