using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class UserQryConfiguration : IEntityTypeConfiguration<UserQry>
    {
        public void Configure(EntityTypeBuilder<UserQry> builder)
        {
            builder.ToTable("UserQry");
            builder.HasKey(e => e.Id);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.Code).HasColumnType("nvarchar(16)").HasMaxLength(16).IsRequired();
            builder.Property(e => e.Name).HasColumnType("nvarchar(48)").HasMaxLength(48).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.InvisibleID).HasColumnType("decimal(1, 0)");
            builder.Property(e => e.CompanyID).HasColumnType("decimal(4, 0)");
        }
    }
}
