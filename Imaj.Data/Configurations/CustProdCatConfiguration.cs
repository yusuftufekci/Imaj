using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class CustProdCatConfiguration : IEntityTypeConfiguration<CustProdCat>
    {
        public void Configure(EntityTypeBuilder<CustProdCat> builder)
        {
            builder.ToTable("CustProdCat");
            
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(10, 0)");

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.CustomerID).HasColumnType("decimal(8, 0)").IsRequired();
            builder.Property(e => e.ProdCatID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.DiscPercentage).HasColumnType("tinyint").IsRequired();
            builder.Property(e => e.Deleted).HasColumnType("decimal(10, 0)").IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.Customer)
                .WithMany()
                .HasForeignKey(d => d.CustomerID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.ProdCat)
                .WithMany()
                .HasForeignKey(d => d.ProdCatID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
