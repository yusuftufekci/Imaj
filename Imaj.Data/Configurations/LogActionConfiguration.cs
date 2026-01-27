using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class LogActionConfiguration : IEntityTypeConfiguration<LogAction>
    {
        public void Configure(EntityTypeBuilder<LogAction> builder)
        {
            builder.ToTable("LogAction");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(4, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.SrvName).HasMaxLength(32).IsRequired();
            builder.Property(e => e.Descr).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
        }
    }
}
