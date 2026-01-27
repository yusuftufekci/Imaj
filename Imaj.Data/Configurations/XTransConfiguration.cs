using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class XTransConfiguration : IEntityTypeConfiguration<XTrans>
    {
        public void Configure(EntityTypeBuilder<XTrans> builder)
        {
            builder.ToTable("XTrans");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(10, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.TransID).HasColumnType("decimal(8, 0)").IsRequired();
            builder.Property(e => e.LanguageID).HasColumnType("decimal(2, 0)").IsRequired();
            builder.Property(e => e.Descr).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.Help).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.Remark).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.Trans)
                .WithMany()
                .HasForeignKey(d => d.TransID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.Language)
                .WithMany()
                .HasForeignKey(d => d.LanguageID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
