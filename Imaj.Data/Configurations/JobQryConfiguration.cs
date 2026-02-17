using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class JobQryConfiguration : IEntityTypeConfiguration<JobQry>
    {
        public void Configure(EntityTypeBuilder<JobQry> builder)
        {
            builder.ToTable("JobQry");
            builder.HasKey(e => e.Id);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.CompanyID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.OwnUserID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.Name).HasColumnType("nvarchar(32)").HasMaxLength(32).IsRequired();
            builder.Property(e => e.Contact).HasColumnType("nvarchar(32)").HasMaxLength(32).IsRequired();
            builder.Property(e => e.ReferenceList).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.FixedCustomer).HasColumnType("bit").IsRequired();
            builder.Property(e => e.FixedState).HasColumnType("bit").IsRequired();
            builder.Property(e => e.FixedEvaluated).HasColumnType("bit").IsRequired();
            builder.Property(e => e.WorkTypeID).HasColumnType("decimal(6, 0)");
            builder.Property(e => e.TimeTypeID).HasColumnType("decimal(6, 0)");
            builder.Property(e => e.EmployeeID).HasColumnType("decimal(6, 0)");
            builder.Property(e => e.ProductID).HasColumnType("decimal(6, 0)");
            builder.Property(e => e.FunctionID).HasColumnType("decimal(6, 0)");
            builder.Property(e => e.CustomerID).HasColumnType("decimal(8, 0)");
            builder.Property(e => e.StateID).HasColumnType("decimal(4, 0)");
            builder.Property(e => e.EvaluatedID).HasColumnType("decimal(1, 0)");
            builder.Property(e => e.MailedID).HasColumnType("decimal(1, 0)");
            builder.Property(e => e.Reference1).HasColumnType("int");
            builder.Property(e => e.Reference2).HasColumnType("int");
            builder.Property(e => e.StartDate1).HasColumnType("smalldatetime");
            builder.Property(e => e.EndDate1).HasColumnType("smalldatetime");
            builder.Property(e => e.StartDate2).HasColumnType("smalldatetime");
            builder.Property(e => e.EndDate2).HasColumnType("smalldatetime");
        }
    }
}
