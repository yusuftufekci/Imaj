using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class ReserveQryConfiguration : IEntityTypeConfiguration<ReserveQry>
    {
        public void Configure(EntityTypeBuilder<ReserveQry> builder)
        {
            builder.ToTable("ReserveQry");
            builder.HasKey(e => e.Id);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.CompanyID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.OwnUserID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.Name).HasColumnType("nvarchar(32)").HasMaxLength(32).IsRequired();
            builder.Property(e => e.Contact).HasColumnType("nvarchar(32)").HasMaxLength(32).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.ResourceID).HasColumnType("decimal(8, 0)");
            builder.Property(e => e.EvaluatedID).HasColumnType("decimal(1, 0)");
            builder.Property(e => e.CustomerID).HasColumnType("decimal(8, 0)");
            builder.Property(e => e.ReasonID).HasColumnType("decimal(6, 0)");
            builder.Property(e => e.StateID).HasColumnType("decimal(4, 0)");
            builder.Property(e => e.AbsenceID).HasColumnType("decimal(1, 0)");
            builder.Property(e => e.FunctionID).HasColumnType("decimal(6, 0)");
            builder.Property(e => e.StartDate1).HasColumnType("smalldatetime");
            builder.Property(e => e.StartDate2).HasColumnType("smalldatetime");
            builder.Property(e => e.EndDate1).HasColumnType("smalldatetime");
            builder.Property(e => e.EndDate2).HasColumnType("smalldatetime");
        }
    }
}
