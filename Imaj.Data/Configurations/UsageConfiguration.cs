using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class UsageConfiguration : IEntityTypeConfiguration<Usage>
    {
        public void Configure(EntityTypeBuilder<Usage> builder)
        {
            builder.ToTable("Usage");
            builder.HasKey(e => e.Id);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.QryName).HasColumnType("nvarchar(32)").HasMaxLength(32).IsRequired();
            builder.Property(e => e.Descr).HasColumnType("ntext").IsRequired();
        }
    }
}
