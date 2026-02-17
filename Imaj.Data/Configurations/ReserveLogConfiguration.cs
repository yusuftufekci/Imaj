using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class ReserveLogConfiguration : IEntityTypeConfiguration<ReserveLog>
    {
        public void Configure(EntityTypeBuilder<ReserveLog> builder)
        {
            builder.ToTable("ReserveLog");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(12, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.ReserveID).HasColumnType("decimal(10, 0)").IsRequired();
            builder.Property(e => e.UserID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.LogActionID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.ActionDT).HasColumnType("smalldatetime").IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.Reserve)
                .WithMany()
                .HasForeignKey(d => d.ReserveID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
