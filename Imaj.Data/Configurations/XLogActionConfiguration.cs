using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class XLogActionConfiguration : IEntityTypeConfiguration<XLogAction>
    {
        public void Configure(EntityTypeBuilder<XLogAction> builder)
        {
            builder.ToTable("XLogAction");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(6, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.LogActionID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.LanguageID).HasColumnType("decimal(2, 0)").IsRequired();
            builder.Property(e => e.Name).HasMaxLength(32).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.LogAction)
                .WithMany()
                .HasForeignKey(d => d.LogActionID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.Language)
                .WithMany()
                .HasForeignKey(d => d.LanguageID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
