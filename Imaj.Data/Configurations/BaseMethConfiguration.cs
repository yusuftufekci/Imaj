using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class BaseMethConfiguration : IEntityTypeConfiguration<BaseMeth>
    {
        public void Configure(EntityTypeBuilder<BaseMeth> builder)
        {
            builder.ToTable("BaseMeth");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(6, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Name).HasMaxLength(32).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
        }
    }
}
