using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class MsgLogConfiguration : IEntityTypeConfiguration<MsgLog>
    {
        public void Configure(EntityTypeBuilder<MsgLog> builder)
        {
            builder.ToTable("MsgLog");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(10, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.LogDT).HasColumnType("smalldatetime").IsRequired();
            builder.Property(e => e.SenderID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.ReceiverID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.Message).HasMaxLength(256).IsRequired();
            builder.Property(e => e.ReadFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.Sender)
                .WithMany()
                .HasForeignKey(d => d.SenderID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.Receiver)
                .WithMany()
                .HasForeignKey(d => d.ReceiverID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
