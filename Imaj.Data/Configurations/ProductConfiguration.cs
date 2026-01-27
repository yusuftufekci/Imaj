using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Product");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(6, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.CompanyID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.ProdCatID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.ProdGrpID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.Code).HasMaxLength(8).IsRequired();
            builder.Property(e => e.Price).HasColumnType("decimal(16, 2)").IsRequired();
            builder.Property(e => e.Invisible).IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.SelectQty).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.ProdCat)
                .WithMany()
                .HasForeignKey(d => d.ProdCatID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.ProdGrp)
                .WithMany()
                .HasForeignKey(d => d.ProdGrpID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
