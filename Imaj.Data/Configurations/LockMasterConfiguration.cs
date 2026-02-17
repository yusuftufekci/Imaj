using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class LockMasterConfiguration : IEntityTypeConfiguration<LockMaster>
    {
        public void Configure(EntityTypeBuilder<LockMaster> builder)
        {
            builder.ToTable("LockMaster");
            builder.HasKey(e => e.ID);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);
            builder.Ignore(e => e.Id);

            builder.Property(e => e.ID).HasColumnType("uniqueidentifier").IsRequired();
            builder.Property(e => e.InstanceID).HasColumnType("uniqueidentifier").IsRequired();
            builder.Property(e => e.SessionID).HasColumnType("uniqueidentifier").IsRequired();
            builder.Property(e => e.ServiceName).HasColumnType("varchar(32)").HasMaxLength(32).IsRequired();
            builder.Property(e => e.LastAccess).HasColumnType("datetime").IsRequired();
            builder.Property(e => e.Timeout).HasColumnType("smallint").IsRequired();
        }
    }
}
