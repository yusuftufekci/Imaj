using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class TimeTypeConfiguration : IEntityTypeConfiguration<TimeType>
    {
        public void Configure(EntityTypeBuilder<TimeType> builder)
        {
            builder.ToTable("TimeType");
            
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(18, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.CompanyID).HasColumnType("decimal(18, 0)").IsRequired();
            builder.Property(e => e.Invisible).IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            // Relationships
            builder.HasOne(d => d.Company)
                .WithMany(p => p.TimeTypes)
                .HasForeignKey(d => d.CompanyID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
