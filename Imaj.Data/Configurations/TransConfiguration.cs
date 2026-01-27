using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class TransConfiguration : IEntityTypeConfiguration<Trans>
    {
        public void Configure(EntityTypeBuilder<Trans> builder)
        {
            builder.ToTable("Trans");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(8, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Reference).IsRequired();
            builder.Property(e => e.TransCatID).HasColumnType("decimal(2, 0)").IsRequired();
            builder.Property(e => e.Name).HasMaxLength(48).IsRequired();
            builder.Property(e => e.TransTypeID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.Size).HasMaxLength(8).IsRequired();
            builder.Property(e => e.Keyword).HasMaxLength(64).IsRequired();
            builder.Property(e => e.ParamCount).HasColumnType("tinyint").IsRequired();
            builder.Property(e => e.ParamHelp).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.Invisible).IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.TransCat)
                .WithMany()
                .HasForeignKey(d => d.TransCatID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.TransType)
                .WithMany()
                .HasForeignKey(d => d.TransTypeID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
