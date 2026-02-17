using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class CompanyQryConfiguration : IEntityTypeConfiguration<CompanyQry>
    {
        public void Configure(EntityTypeBuilder<CompanyQry> builder)
        {
            builder.ToTable("CompanyQry");
            builder.HasKey(e => e.Id);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.Name).HasColumnType("nvarchar(32)").HasMaxLength(32).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.InvisibleID).HasColumnType("decimal(1, 0)");
        }
    }
}
