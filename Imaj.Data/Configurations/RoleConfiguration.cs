using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Role");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(4, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Name).HasMaxLength(32).IsRequired();
            builder.Property(e => e.Notes).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.AllPropRead).IsRequired();
            builder.Property(e => e.AllPropWrite).IsRequired();
            builder.Property(e => e.AllMethRead).IsRequired();
            builder.Property(e => e.AllMethWrite).IsRequired();
            builder.Property(e => e.AllMenu).IsRequired();
            builder.Property(e => e.Invisible).IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Global).HasColumnName("Global").IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
        }
    }
}
