using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class InvoiceLogConfiguration : IEntityTypeConfiguration<InvoiceLog>
    {
        public void Configure(EntityTypeBuilder<InvoiceLog> builder)
        {
            builder.ToTable("InvoiceLog");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(14, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.InvoiceID).HasColumnType("decimal(10, 0)").IsRequired();
            builder.Property(e => e.LogDT).HasColumnName("ActionDT").HasColumnType("smalldatetime").IsRequired();
            builder.Property(e => e.LogActionID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.UserID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.Machine).HasMaxLength(32).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.Invoice)
                .WithMany()
                .HasForeignKey(d => d.InvoiceID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.LogAction)
                .WithMany()
                .HasForeignKey(d => d.LogActionID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
