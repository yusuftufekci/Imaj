using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class RoleMethConfiguration : IEntityTypeConfiguration<RoleMeth>
    {
        public void Configure(EntityTypeBuilder<RoleMeth> builder)
        {
            builder.ToTable("RoleMeth");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(8, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.BaseMethID).HasColumnType("decimal(8, 0)").IsRequired();
            builder.Property(e => e.RoleContID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.Deleted).HasColumnType("decimal(8, 0)").IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.BaseMeth)
                .WithMany()
                .HasForeignKey(d => d.BaseMethID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.RoleCont)
                .WithMany()
                .HasForeignKey(d => d.RoleContID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasIndex(e => new { e.BaseMethID, e.RoleContID, e.Deleted })
                .IsUnique();
        }
    }
}
