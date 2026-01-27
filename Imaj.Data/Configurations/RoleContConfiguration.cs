using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class RoleContConfiguration : IEntityTypeConfiguration<RoleCont>
    {
        public void Configure(EntityTypeBuilder<RoleCont> builder)
        {
            builder.ToTable("RoleCont");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(6, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.RoleID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.BaseContID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.AllPropRead).IsRequired();
            builder.Property(e => e.AllPropWrite).IsRequired();
            builder.Property(e => e.AllMethRead).IsRequired();
            builder.Property(e => e.AllMethWrite).IsRequired();
            builder.Property(e => e.AllIntf).IsRequired();
            builder.Property(e => e.Deleted).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.Role)
                .WithMany()
                .HasForeignKey(d => d.RoleID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.BaseCont)
                .WithMany()
                .HasForeignKey(d => d.BaseContID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
