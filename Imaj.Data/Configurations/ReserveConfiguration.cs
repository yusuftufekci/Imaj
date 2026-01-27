using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class ReserveConfiguration : IEntityTypeConfiguration<Reserve>
    {
        public void Configure(EntityTypeBuilder<Reserve> builder)
        {
            builder.ToTable("Reserve");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(10, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.CompanyID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.FunctionID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.StateID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.Name).HasMaxLength(32).IsRequired();
            builder.Property(e => e.Contact).HasMaxLength(32).IsRequired();
            builder.Property(e => e.Notes).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.StartDT).HasColumnType("smalldatetime").IsRequired();
            builder.Property(e => e.EndDT).HasColumnType("smalldatetime").IsRequired();
            builder.Property(e => e.Absence).IsRequired();
            builder.Property(e => e.Evaluated).IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
            
            builder.Property(e => e.CustomerID).HasColumnType("decimal(8, 0)");
            builder.Property(e => e.ReasonID).HasColumnType("decimal(6, 0)");

            builder.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.Function)
                .WithMany()
                .HasForeignKey(d => d.FunctionID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.State)
                .WithMany()
                .HasForeignKey(d => d.StateID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.Customer)
                .WithMany()
                .HasForeignKey(d => d.CustomerID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.Reason)
                .WithMany()
                .HasForeignKey(d => d.ReasonID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
