using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class ProdCatConfiguration : IEntityTypeConfiguration<ProdCat>
    {
        public void Configure(EntityTypeBuilder<ProdCat> builder)
        {
            builder.ToTable("ProdCat");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(6, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.CompanyID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.TaxTypeID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.Invisible).IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Sequence).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            // Relationships
            builder.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.TaxType)
                .WithMany()
                .HasForeignKey(d => d.TaxTypeID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
