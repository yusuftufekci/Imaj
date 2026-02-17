using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class RoleIntfConfiguration : IEntityTypeConfiguration<RoleIntf>
    {
        public void Configure(EntityTypeBuilder<RoleIntf> builder)
        {
            builder.ToTable("RoleIntf");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(6, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.RoleContID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.BaseIntfID).HasColumnType("decimal(8, 0)").IsRequired();
            builder.Property(e => e.Deleted).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.RoleCont)
                .WithMany()
                .HasForeignKey(d => d.RoleContID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.BaseIntf)
                .WithMany()
                .HasForeignKey(d => d.BaseIntfID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
