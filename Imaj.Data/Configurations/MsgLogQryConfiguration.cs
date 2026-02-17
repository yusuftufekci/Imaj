using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class MsgLogQryConfiguration : IEntityTypeConfiguration<MsgLogQry>
    {
        public void Configure(EntityTypeBuilder<MsgLogQry> builder)
        {
            builder.ToTable("MsgLogQry");
            builder.HasKey(e => e.Id);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.Interface).HasColumnType("nvarchar(32)").HasMaxLength(32).IsRequired();
            builder.Property(e => e.Controller).HasColumnType("nvarchar(16)").HasMaxLength(16).IsRequired();
            builder.Property(e => e.Method).HasColumnType("nvarchar(64)").HasMaxLength(64).IsRequired();
            builder.Property(e => e.Server).HasColumnType("nvarchar(32)").HasMaxLength(32).IsRequired();
            builder.Property(e => e.UserCode).HasColumnType("nvarchar(16)").HasMaxLength(16).IsRequired();
            builder.Property(e => e.MsgSessionID).HasColumnType("nvarchar(48)").HasMaxLength(48).IsRequired();
            builder.Property(e => e.MsgInstanceID).HasColumnType("nvarchar(48)").HasMaxLength(48).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.Number1).HasColumnType("int");
            builder.Property(e => e.Number2).HasColumnType("int");
            builder.Property(e => e.ActionMethodID).HasColumnType("decimal(1, 0)");
            builder.Property(e => e.TurkuazID).HasColumnType("decimal(1, 0)");
            builder.Property(e => e.LogDT1).HasColumnType("smalldatetime");
            builder.Property(e => e.LogDT2).HasColumnType("smalldatetime");
        }
    }
}
