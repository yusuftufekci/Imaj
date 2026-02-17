using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class JobProdQryConfiguration : IEntityTypeConfiguration<JobProdQry>
    {
        public void Configure(EntityTypeBuilder<JobProdQry> builder)
        {
            builder.ToTable("JobProdQry");
            builder.HasKey(e => e.Id);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.CompanyID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.JobStateIDList).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.StartDate1).HasColumnType("smalldatetime");
            builder.Property(e => e.StartDate2).HasColumnType("smalldatetime");
            builder.Property(e => e.ProductID).HasColumnType("decimal(6, 0)");
            builder.Property(e => e.CustomerID).HasColumnType("decimal(8, 0)");
            builder.Property(e => e.ProdGrpID).HasColumnType("decimal(6, 0)");
        }
    }
}
