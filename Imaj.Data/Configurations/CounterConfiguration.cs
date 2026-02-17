using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class CounterConfiguration : IEntityTypeConfiguration<Counter>
    {
        public void Configure(EntityTypeBuilder<Counter> builder)
        {
            builder.ToTable("Counter");
            builder.HasKey(e => e.Name);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);
            builder.Ignore(e => e.Id);

            builder.Property(e => e.Name).HasColumnType("varchar(32)").HasMaxLength(32).IsRequired();
            builder.Property(e => e.CounterValue).HasColumnName("Counter").HasColumnType("decimal(16, 0)").IsRequired();
        }
    }
}
