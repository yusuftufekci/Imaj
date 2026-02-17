using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class InvoiceQryConfiguration : IEntityTypeConfiguration<InvoiceQry>
    {
        public void Configure(EntityTypeBuilder<InvoiceQry> builder)
        {
            builder.ToTable("InvoiceQry");
            builder.HasKey(e => e.Id);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.CompanyID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.Name).HasColumnType("nvarchar(32)").HasMaxLength(32).IsRequired();
            builder.Property(e => e.Contact).HasColumnType("nvarchar(32)").HasMaxLength(32).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.Reference1).HasColumnType("int");
            builder.Property(e => e.Reference2).HasColumnType("int");
            builder.Property(e => e.InvoCustomerID).HasColumnType("decimal(8, 0)");
            builder.Property(e => e.JobCustomerID).HasColumnType("decimal(8, 0)");
            builder.Property(e => e.StateID).HasColumnType("decimal(4, 0)");
            builder.Property(e => e.EvaluatedID).HasColumnType("decimal(1, 0)");
            builder.Property(e => e.IssueDate1).HasColumnType("smalldatetime");
            builder.Property(e => e.IssueDate2).HasColumnType("smalldatetime");
        }
    }
}
