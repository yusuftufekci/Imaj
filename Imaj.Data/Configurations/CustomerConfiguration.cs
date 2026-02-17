using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("Customer");
            builder.HasKey(e => e.Id);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id)
                .HasColumnName("ID")
                .HasColumnType("decimal(8, 0)");
            
            builder.Property(e => e.CompanyID)
                .HasColumnType("decimal(4, 0)")
                .IsRequired();

            builder.Property(e => e.Code).HasMaxLength(8).IsRequired();
            builder.Property(e => e.Name).HasMaxLength(32).IsRequired();
            builder.Property(e => e.InvoName).HasMaxLength(64).IsRequired();
            builder.Property(e => e.Notes).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.Owner).HasMaxLength(32).IsRequired();
            builder.Property(e => e.Contact).HasMaxLength(32).IsRequired();
            builder.Property(e => e.Phone).HasMaxLength(32).IsRequired();
            builder.Property(e => e.Fax).HasMaxLength(32).IsRequired();
            builder.Property(e => e.EMail).HasMaxLength(64).IsRequired();
            builder.Property(e => e.Address).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.City).HasMaxLength(32).IsRequired();
            builder.Property(e => e.Zip).HasMaxLength(32).IsRequired();
            builder.Property(e => e.Country).HasMaxLength(32).IsRequired();
            builder.Property(e => e.TaxOffice).HasMaxLength(32).IsRequired();
            builder.Property(e => e.TaxNumber).HasMaxLength(32).IsRequired();
            builder.Property(e => e.Invisible).IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasIndex(e => new { e.CompanyID, e.Code })
                .IsUnique();

            builder.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
