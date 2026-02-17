using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class TransQryConfiguration : IEntityTypeConfiguration<TransQry>
    {
        public void Configure(EntityTypeBuilder<TransQry> builder)
        {
            builder.ToTable("TransQry");
            builder.HasKey(e => e.Id);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.Name).HasColumnType("nvarchar(48)").HasMaxLength(48).IsRequired();
            builder.Property(e => e.Descr).HasColumnType("nvarchar(64)").HasMaxLength(64).IsRequired();
            builder.Property(e => e.Remark).HasColumnType("nvarchar(64)").HasMaxLength(64).IsRequired();
            builder.Property(e => e.Help).HasColumnType("nvarchar(64)").HasMaxLength(64).IsRequired();
            builder.Property(e => e.Size).HasColumnType("nvarchar(8)").HasMaxLength(8).IsRequired();
            builder.Property(e => e.Keyword).HasColumnType("nvarchar(64)").HasMaxLength(64).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.Reference).HasColumnType("int");
            builder.Property(e => e.TransCatID).HasColumnType("decimal(2, 0)");
            builder.Property(e => e.TransTypeID).HasColumnType("decimal(4, 0)");
            builder.Property(e => e.ParamCount1).HasColumnType("tinyint");
            builder.Property(e => e.ParamCount2).HasColumnType("tinyint");
            builder.Property(e => e.MissingHelpLangID).HasColumnType("decimal(2, 0)");
            builder.Property(e => e.MissingDescrLangID).HasColumnType("decimal(2, 0)");
            builder.Property(e => e.MissingRemarkLangID).HasColumnType("decimal(2, 0)");
            builder.Property(e => e.InvisibleID).HasColumnType("decimal(1, 0)");
            builder.Property(e => e.DescrLangID).HasColumnType("decimal(2, 0)");
            builder.Property(e => e.RemarkLangID).HasColumnType("decimal(2, 0)");
            builder.Property(e => e.HelpLangID).HasColumnType("decimal(2, 0)");
        }
    }
}
