using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class ResoCatConfiguration : IEntityTypeConfiguration<ResoCat>
    {
        public void Configure(EntityTypeBuilder<ResoCat> builder)
        {
            builder.ToTable("ResoCat");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(6, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.CompanyID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.Invisible).IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
