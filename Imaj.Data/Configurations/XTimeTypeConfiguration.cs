using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class XTimeTypeConfiguration : IEntityTypeConfiguration<XTimeType>
    {
        public void Configure(EntityTypeBuilder<XTimeType> builder)
        {
            builder.ToTable("XTimeType");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(8, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.TimeTypeID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.LanguageID).HasColumnType("decimal(2, 0)").IsRequired();
            builder.Property(e => e.Name).HasMaxLength(32).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.TimeType)
                .WithMany()
                .HasForeignKey(d => d.TimeTypeID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.Language)
                .WithMany()
                .HasForeignKey(d => d.LanguageID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
