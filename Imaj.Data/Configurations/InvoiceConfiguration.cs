using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.ToTable("Invoice");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(10, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.CompanyID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.JobCustomerID).HasColumnType("decimal(8, 0)").IsRequired();
            builder.Property(e => e.InvoCustomerID).HasColumnType("decimal(8, 0)").IsRequired();
            builder.Property(e => e.StateID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.Reference).IsRequired();
            builder.Property(e => e.Name).HasMaxLength(32).IsRequired();
            builder.Property(e => e.Contact).HasMaxLength(32).IsRequired();
            builder.Property(e => e.Notes).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.Footer).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.NetAmount).HasColumnType("decimal(16, 2)").IsRequired();
            builder.Property(e => e.TaxAmount).HasColumnType("decimal(16, 2)").IsRequired();
            builder.Property(e => e.GrossAmount).HasColumnType("decimal(16, 2)").IsRequired();
            builder.Property(e => e.Evaluated).IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.IssueDate).HasColumnType("smalldatetime");

            builder.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.JobCustomer)
                .WithMany() // Assuming Customer has collection, skipping for now
                .HasForeignKey(d => d.JobCustomerID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.InvoCustomer)
                .WithMany()
                .HasForeignKey(d => d.InvoCustomerID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.State)
                .WithMany()
                .HasForeignKey(d => d.StateID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
