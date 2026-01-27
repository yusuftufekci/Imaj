using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class ReasonConfiguration : IEntityTypeConfiguration<Reason>
    {
        public void Configure(EntityTypeBuilder<Reason> builder)
        {
            builder.ToTable("Reason");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(6, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.CompanyID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.ReasonCatID).HasColumnType("decimal(2, 0)").IsRequired();
            builder.Property(e => e.Code).HasMaxLength(8).IsRequired();
            builder.Property(e => e.Invisible).IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            // Relationships
            builder.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.ReasonCat)
                .WithMany(p => p.Reasons)
                .HasForeignKey(d => d.ReasonCatID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
