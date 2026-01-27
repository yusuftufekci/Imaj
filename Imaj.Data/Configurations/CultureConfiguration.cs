using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class CultureConfiguration : IEntityTypeConfiguration<Culture>
    {
        public void Configure(EntityTypeBuilder<Culture> builder)
        {
            builder.ToTable("Culture");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(4, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Name).HasMaxLength(32).IsRequired();
            builder.Property(e => e.DateSep).HasMaxLength(1).IsRequired();
            builder.Property(e => e.TimeSep).HasMaxLength(1).IsRequired();
            builder.Property(e => e.DigitSep).HasMaxLength(1).IsRequired();
            builder.Property(e => e.DecimalSep).HasMaxLength(1).IsRequired();
            builder.Property(e => e.FormattedNumeric).IsRequired();
            builder.Property(e => e.PadWithZero).IsRequired();
            builder.Property(e => e.MoneyWidth).HasColumnType("tinyint").IsRequired();
            builder.Property(e => e.MoneyDecimals).HasColumnType("tinyint");
        }
    }
}
