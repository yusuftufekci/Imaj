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
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(16, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.UserID).HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.LogDT).HasColumnType("smalldatetime").IsRequired();
            builder.Property(e => e.MsgSessionID).HasColumnType("uniqueidentifier").IsRequired();
            builder.Property(e => e.MsgInstanceID).HasColumnType("uniqueidentifier").IsRequired();
            builder.Property(e => e.Interface).HasMaxLength(32).IsRequired();
            builder.Property(e => e.Controller).HasMaxLength(16).IsRequired();
            builder.Property(e => e.Method).HasMaxLength(64).IsRequired();
            builder.Property(e => e.Server).HasMaxLength(32).IsRequired();
            builder.Property(e => e.Number).HasColumnType("int").IsRequired();
            builder.Property(e => e.Description).HasMaxLength(2048).IsRequired();
            builder.Property(e => e.Source).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.CallCount).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.MemberID).HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.ActionMethod).IsRequired();
            builder.Property(e => e.Turkuaz).IsRequired();
            builder.Property(e => e.MsgType).HasColumnType("tinyint").IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
        }
    }
}
