using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class JobWorkQryEmpConfiguration : IEntityTypeConfiguration<JobWorkQryEmp>
    {
        public void Configure(EntityTypeBuilder<JobWorkQryEmp> builder)
        {
            builder.ToTable("JobWorkQryEmp");
            builder.HasKey(e => e.Id);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.JobWorkQryID).HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.EmployeeID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.Deleted).HasColumnType("decimal(18, 0)").IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
        }
    }
}
