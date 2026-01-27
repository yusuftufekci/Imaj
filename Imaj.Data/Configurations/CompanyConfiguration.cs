using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class CompanyConfiguration : IEntityTypeConfiguration<Company>
    {
        public void Configure(EntityTypeBuilder<Company> builder)
        {
            builder.ToTable("Company");
            
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(18, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Name).HasMaxLength(32).IsRequired();
            builder.Property(e => e.MaxReserveDay).HasColumnType("tinyint").IsRequired();
            builder.Property(e => e.CalenderHourOffset).HasColumnType("tinyint").IsRequired();
            builder.Property(e => e.JobReportName).HasMaxLength(48).IsRequired();
            builder.Property(e => e.InvoiceReportName).HasMaxLength(48).IsRequired();
            builder.Property(e => e.LabelReportName).HasMaxLength(48).IsRequired();
            builder.Property(e => e.MailServer).HasMaxLength(32).IsRequired();
            builder.Property(e => e.MailUser).HasMaxLength(32).IsRequired();
            builder.Property(e => e.MailPassword).HasMaxLength(32).IsRequired();
            builder.Property(e => e.MailAddress).HasMaxLength(64).IsRequired();
            builder.Property(e => e.MailPath).HasMaxLength(128).IsRequired();
            builder.Property(e => e.ReportPath).HasMaxLength(128).IsRequired();
            builder.Property(e => e.Footer).HasColumnType("ntext").IsRequired(); // ntext maps to string by default but explicit type is good
            
            builder.Property(e => e.Invisible).IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
        }
    }
}
