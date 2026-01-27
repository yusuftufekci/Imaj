using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class ResourceConfiguration : IEntityTypeConfiguration<Resource>
    {
        public void Configure(EntityTypeBuilder<Resource> builder)
        {
            builder.ToTable("Resource");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(8, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.CompanyID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.FunctionID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.ResoCatID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.Sequence).IsRequired();
            builder.Property(e => e.Code).HasMaxLength(8).IsRequired();
            builder.Property(e => e.Invisible).IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.Function)
                .WithMany()
                .HasForeignKey(d => d.FunctionID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.ResoCat)
                .WithMany()
                .HasForeignKey(d => d.ResoCatID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
