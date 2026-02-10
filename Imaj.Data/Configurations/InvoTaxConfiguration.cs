using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class InvoTaxConfiguration : IEntityTypeConfiguration<InvoTax>
    {
        public void Configure(EntityTypeBuilder<InvoTax> builder)
        {
            builder.ToTable("InvoTax");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(12, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.InvoiceID).HasColumnType("decimal(10, 0)").IsRequired();
            builder.Property(e => e.TaxTypeID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.GrossAmount).HasColumnType("decimal(16, 2)").IsRequired();
            builder.Property(e => e.TaxPercentage).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.TaxAmount).HasColumnType("decimal(16, 2)").IsRequired();
            builder.Property(e => e.NetAmount).HasColumnType("decimal(16, 2)").IsRequired();
            builder.Property(e => e.Deleted).HasColumnType("decimal(12, 0)").IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.Invoice)
                .WithMany()
                .HasForeignKey(d => d.InvoiceID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.TaxType)
                .WithMany()
                .HasForeignKey(d => d.TaxTypeID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
