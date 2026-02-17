using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class RoleQryConfiguration : IEntityTypeConfiguration<RoleQry>
    {
        public void Configure(EntityTypeBuilder<RoleQry> builder)
        {
            builder.ToTable("RoleQry");
            builder.HasKey(e => e.Id);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.Name).HasColumnType("nvarchar(32)").HasMaxLength(32).IsRequired();
            builder.Property(e => e.ExceptIDList).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.InvisibleID).HasColumnType("decimal(1, 0)");
            builder.Property(e => e.GlobalID).HasColumnType("decimal(1, 0)");
            builder.Property(e => e.AllMenuID).HasColumnType("decimal(1, 0)");
            builder.Property(e => e.AllMethReadID).HasColumnType("decimal(1, 0)");
            builder.Property(e => e.AllMethWriteID).HasColumnType("decimal(1, 0)");
            builder.Property(e => e.AllPropReadID).HasColumnType("decimal(1, 0)");
            builder.Property(e => e.AllPropWriteID).HasColumnType("decimal(1, 0)");
            builder.Property(e => e.ContAllIntfID).HasColumnType("decimal(1, 0)");
            builder.Property(e => e.ContAllMethReadID).HasColumnType("decimal(1, 0)");
            builder.Property(e => e.ContAllMethWriteID).HasColumnType("decimal(1, 0)");
            builder.Property(e => e.ContAllPropReadID).HasColumnType("decimal(1, 0)");
            builder.Property(e => e.ContAllPropWriteID).HasColumnType("decimal(1, 0)");
            builder.Property(e => e.ContID).HasColumnType("decimal(6, 0)");
            builder.Property(e => e.MenuID).HasColumnType("decimal(8, 0)");
        }
    }
}
