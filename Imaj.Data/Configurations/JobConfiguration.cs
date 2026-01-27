using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class JobConfiguration : IEntityTypeConfiguration<Job>
    {
        public void Configure(EntityTypeBuilder<Job> builder)
        {
            builder.ToTable("Job");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(10, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.CompanyID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.FunctionID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.CustomerID).HasColumnType("decimal(8, 0)").IsRequired();
            builder.Property(e => e.StateID).HasColumnType("decimal(4, 0)").IsRequired();
            
            builder.Property(e => e.ProdSum).HasColumnType("decimal(16, 2)").IsRequired();
            builder.Property(e => e.WorkSum).HasColumnType("decimal(16, 2)").IsRequired();
            builder.Property(e => e.Reference).IsRequired();
            
            builder.Property(e => e.StartDT).HasColumnType("smalldatetime").IsRequired();
            builder.Property(e => e.EndDT).HasColumnType("smalldatetime").IsRequired();
            
            builder.Property(e => e.Name).HasMaxLength(32).IsRequired();
            builder.Property(e => e.Contact).HasMaxLength(32).IsRequired();
            builder.Property(e => e.IntNotes).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.ExtNotes).HasColumnType("ntext").IsRequired();
            
            builder.Property(e => e.Evaluated).IsRequired();
            builder.Property(e => e.Mailed).IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
            
            builder.Property(e => e.InvoLineID).HasColumnType("decimal(12, 0)");

            builder.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.Function)
                .WithMany()
                .HasForeignKey(d => d.FunctionID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.Customer)
                .WithMany()
                .HasForeignKey(d => d.CustomerID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.State)
                .WithMany()
                .HasForeignKey(d => d.StateID)
                .OnDelete(DeleteBehavior.NoAction);
            
            builder.HasOne(d => d.InvoLine)
                .WithMany()
                .HasForeignKey(d => d.InvoLineID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
