using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class TransCatConfiguration : IEntityTypeConfiguration<TransCat>
    {
        public void Configure(EntityTypeBuilder<TransCat> builder)
        {
            builder.ToTable("TransCat");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(2, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Name).HasMaxLength(16).IsRequired();
        }
    }
}
