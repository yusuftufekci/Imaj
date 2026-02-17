using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class BaseContQryConfiguration : IEntityTypeConfiguration<BaseContQry>
    {
        public void Configure(EntityTypeBuilder<BaseContQry> builder)
        {
            builder.ToTable("BaseContQry");
            builder.HasKey(e => e.Id);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.ExceptIDList).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
        }
    }
}
