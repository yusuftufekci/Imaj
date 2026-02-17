using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class DtpropertiesConfiguration : IEntityTypeConfiguration<Dtproperties>
    {
        public void Configure(EntityTypeBuilder<Dtproperties> builder)
        {
            builder.ToTable("dtproperties");
            builder.HasKey(e => new { e.id, e.property });

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);
            builder.Ignore(e => e.Id);

            builder.Property(e => e.id).HasColumnType("int").IsRequired();
            builder.Property(e => e.objectid).HasColumnType("int");
            builder.Property(e => e.property).HasColumnType("varchar(64)").HasMaxLength(64).IsRequired();
            builder.Property(e => e.value).HasColumnType("varchar(255)").HasMaxLength(255);
            builder.Property(e => e.uvalue).HasColumnType("nvarchar(255)").HasMaxLength(255);
            builder.Property(e => e.lvalue).HasColumnType("image");
            builder.Property(e => e.version).HasColumnType("int").IsRequired();
        }
    }
}
