using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class SessionQryConfiguration : IEntityTypeConfiguration<SessionQry>
    {
        public void Configure(EntityTypeBuilder<SessionQry> builder)
        {
            builder.ToTable("SessionQry");
            builder.HasKey(e => e.Id);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.StateID).HasColumnType("decimal(4, 0)");
            builder.Property(e => e.SessionID).HasColumnType("uniqueidentifier");
        }
    }
}
