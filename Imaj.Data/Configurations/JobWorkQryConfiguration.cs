using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class JobWorkQryConfiguration : IEntityTypeConfiguration<JobWorkQry>
    {
        public void Configure(EntityTypeBuilder<JobWorkQry> builder)
        {
            builder.ToTable("JobWorkQry");
            builder.HasKey(e => e.Id);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.CompanyID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.OwnUserID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.AllEmployee).HasColumnType("bit").IsRequired();
            builder.Property(e => e.JobStateIDList).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint");
            builder.Property(e => e.CustomerID).HasColumnType("decimal(8, 0)");
            builder.Property(e => e.EmployeeID).HasColumnType("decimal(6, 0)");
            builder.Property(e => e.StartDate1).HasColumnType("smalldatetime");
            builder.Property(e => e.StartDate2).HasColumnType("smalldatetime");
        }
    }
}
