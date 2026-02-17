using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class LanguageQryConfiguration : IEntityTypeConfiguration<LanguageQry>
    {
        public void Configure(EntityTypeBuilder<LanguageQry> builder)
        {
            builder.ToTable("LanguageQry");
            builder.HasKey(e => e.Id);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.ExceptIDList).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
        }
    }
}
