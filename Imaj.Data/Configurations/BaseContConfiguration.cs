using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class BaseContConfiguration : IEntityTypeConfiguration<BaseCont>
    {
        public void Configure(EntityTypeBuilder<BaseCont> builder)
        {
            builder.ToTable("BaseCont");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(6, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Name).HasMaxLength(32).IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
        }
    }
}
