using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class QueryConfiguration : IEntityTypeConfiguration<Query>
    {
        public void Configure(EntityTypeBuilder<Query> builder)
        {
            builder.ToTable("Query");
            builder.HasKey(e => e.Id);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.UserID).HasColumnType("decimal(8, 0)").IsRequired();
            builder.Property(e => e.TableName).HasColumnType("varchar(32)").HasMaxLength(32).IsRequired();
            builder.Property(e => e.ExceptIDList).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.OwnName).HasColumnType("nvarchar(64)").HasMaxLength(64).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.UsageID).HasColumnType("decimal(4, 0)");
        }
    }
}
