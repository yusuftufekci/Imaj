using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class RolePropConfiguration : IEntityTypeConfiguration<RoleProp>
    {
        public void Configure(EntityTypeBuilder<RoleProp> builder)
        {
            builder.ToTable("RoleProp");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(6, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.RoleID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.BasePropID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.Readable).IsRequired();
            builder.Property(e => e.Writable).IsRequired();
            builder.Property(e => e.Deleted).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.Role)
                .WithMany()
                .HasForeignKey(d => d.RoleID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.BaseProp)
                .WithMany()
                .HasForeignKey(d => d.BasePropID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
