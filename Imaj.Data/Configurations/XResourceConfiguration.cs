using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class XResourceConfiguration : IEntityTypeConfiguration<XResource>
    {
        public void Configure(EntityTypeBuilder<XResource> builder)
        {
            builder.ToTable("XResource");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(10, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.ResourceID).HasColumnType("decimal(8, 0)").IsRequired();
            builder.Property(e => e.LanguageID).HasColumnType("decimal(2, 0)").IsRequired();
            builder.Property(e => e.Name).HasMaxLength(32).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.Resource)
                .WithMany()
                .HasForeignKey(d => d.ResourceID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.Language)
                .WithMany()
                .HasForeignKey(d => d.LanguageID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
