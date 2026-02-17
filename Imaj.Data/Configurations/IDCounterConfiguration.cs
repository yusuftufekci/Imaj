using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class IDCounterConfiguration : IEntityTypeConfiguration<IDCounter>
    {
        public void Configure(EntityTypeBuilder<IDCounter> builder)
        {
            builder.ToTable("IDCounter");
            builder.HasKey(e => e.TableName);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);
            builder.Ignore(e => e.Id);

            builder.Property(e => e.TableName).HasColumnType("varchar(32)").HasMaxLength(32).IsRequired();
            builder.Property(e => e.Counter).HasColumnType("decimal(16, 0)").IsRequired();
        }
    }
}
