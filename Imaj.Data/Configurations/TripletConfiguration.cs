using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class TripletConfiguration : IEntityTypeConfiguration<Triplet>
    {
        public void Configure(EntityTypeBuilder<Triplet> builder)
        {
            builder.ToTable("Triplet");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(1, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
        }
    }
}
