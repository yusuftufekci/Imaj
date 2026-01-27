using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class InvoProdCatConfiguration : IEntityTypeConfiguration<InvoProdCat>
    {
        public void Configure(EntityTypeBuilder<InvoProdCat> builder)
        {
            builder.ToTable("InvoProdCat");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(10, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.InvoiceID).HasColumnType("decimal(10, 0)").IsRequired();
            builder.Property(e => e.ProdCatID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.Deleted).HasColumnType("decimal(10, 0)").IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.Invoice)
                .WithMany()
                .HasForeignKey(d => d.InvoiceID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.ProdCat)
                .WithMany()
                .HasForeignKey(d => d.ProdCatID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
