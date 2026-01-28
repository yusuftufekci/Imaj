using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class CustProdCatConfiguration : IEntityTypeConfiguration<CustProdCat>
    {
        public void Configure(EntityTypeBuilder<CustProdCat> builder)
        {
            builder.ToTable("CustProdCat"); // Tablo adı tekil
            
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(18, 0)"); // ID tipi veritabanına göre ayarlandı

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.CustomerID).HasColumnName("CustomerId").HasColumnType("decimal(18, 0)").IsRequired();
            builder.Property(e => e.ProdCatID).HasColumnName("ProdCatId").HasColumnType("decimal(18, 0)").IsRequired();
            builder.Property(e => e.Discount).HasColumnName("DiscPercentage").HasColumnType("tinyint").IsRequired(); // DB'den byte geliyor
            builder.Property(e => e.SelectFlag).HasColumnName("SelectFlag").IsRequired();
            builder.Property(e => e.Stamp).HasColumnName("Stamp").HasColumnType("smallint").IsRequired();
            builder.Property(e => e.Deleted).HasColumnName("Deleted").HasColumnType("decimal(18, 0)").IsRequired();

            // İlişkiler (Opsiyonel ama iyi practice)
            builder.HasOne(d => d.Customer)
                .WithMany()
                .HasForeignKey(d => d.CustomerID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.ProdCat)
                .WithMany()
                .HasForeignKey(d => d.ProdCatID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
