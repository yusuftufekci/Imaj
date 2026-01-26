using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    /// <summary>
    /// Customer entity için EF Core konfigürasyonu.
    /// Legacy veritabanı şemasına uygun mapping yapılmıştır.
    /// </summary>
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            // Tablo adı
            builder.ToTable("Customer");
            
            // Primary Key
            builder.HasKey(e => e.Id);
            
            // BaseEntity property'lerini ignore et (legacy şemada yok)
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            // ID - decimal(18, 0) olarak tanımlı
            builder.Property(e => e.Id)
                .HasColumnName("ID")
                .HasColumnType("decimal(18, 0)");
            
            // CompanyID - decimal(18, 0) olarak tanımlı
            builder.Property(e => e.CompanyID)
                .HasColumnType("decimal(18, 0)");

            // String property'ler - MaxLength tanımları
            builder.Property(e => e.Code).HasMaxLength(8);
            builder.Property(e => e.Name).HasMaxLength(32);
            builder.Property(e => e.City).HasMaxLength(32);
            builder.Property(e => e.Phone).HasMaxLength(32);
            builder.Property(e => e.Fax).HasMaxLength(32);
            builder.Property(e => e.EMail).HasMaxLength(64);
            builder.Property(e => e.InvoName).HasMaxLength(64);
            builder.Property(e => e.Contact).HasMaxLength(32);
            builder.Property(e => e.TaxOffice).HasMaxLength(32);
            builder.Property(e => e.TaxNumber).HasMaxLength(32);
            builder.Property(e => e.Country).HasMaxLength(32);
            builder.Property(e => e.Owner).HasMaxLength(32);
            builder.Property(e => e.Zip).HasMaxLength(32);
            
            // ntext alanlar için özel konfigürasyon gerekmez,
            // EF Core bunları otomatik olarak nvarchar(max) olarak ele alır.
            // Gerekirse: builder.Property(e => e.Address).HasColumnType("ntext");
        }
    }
}
