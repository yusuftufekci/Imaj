using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class TransTypeConfiguration : IEntityTypeConfiguration<TransType>
    {
        public void Configure(EntityTypeBuilder<TransType> builder)
        {
            builder.ToTable("TransType");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(4, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Name).HasMaxLength(16).IsRequired();
            builder.Property(e => e.Message).IsRequired();
        }
    }
}
