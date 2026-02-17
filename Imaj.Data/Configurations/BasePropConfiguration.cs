using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class BasePropConfiguration : IEntityTypeConfiguration<BaseProp>
    {
        public void Configure(EntityTypeBuilder<BaseProp> builder)
        {
            builder.ToTable("BaseProp");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(8, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.BaseContID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.Name).HasMaxLength(64).IsRequired();
            builder.Property(e => e.ReadOnly).IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.BaseCont)
                .WithMany()
                .HasForeignKey(d => d.BaseContID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
