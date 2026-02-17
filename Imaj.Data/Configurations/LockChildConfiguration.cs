using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class LockChildConfiguration : IEntityTypeConfiguration<LockChild>
    {
        public void Configure(EntityTypeBuilder<LockChild> builder)
        {
            builder.ToTable("LockChild");
            builder.HasKey(e => new { e.ServiceName, e.ServiceID });

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);
            builder.Ignore(e => e.Id);

            builder.Property(e => e.ID).HasColumnType("uniqueidentifier").IsRequired();
            builder.Property(e => e.ServiceName).HasColumnType("varchar(32)").HasMaxLength(32).IsRequired();
            builder.Property(e => e.ServiceID).HasColumnType("decimal(16, 0)").IsRequired();
        }
    }
}
