using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class JobWorkConfiguration : IEntityTypeConfiguration<JobWork>
    {
        public void Configure(EntityTypeBuilder<JobWork> builder)
        {
            builder.ToTable("JobWork");

            builder.HasKey(e => e.Id);
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id)
                .HasColumnType("decimal(12, 0)");

            builder.Property(e => e.JobID)
                .HasColumnType("decimal(10, 0)")
                .IsRequired();

            builder.Property(e => e.EmployeeID)
                .HasColumnType("decimal(6, 0)")
                .IsRequired();

            builder.Property(e => e.WorkTypeID)
                .HasColumnType("decimal(6, 0)")
                .IsRequired();

            builder.Property(e => e.TimeTypeID)
                .HasColumnType("decimal(6, 0)")
                .IsRequired();

            builder.Property(e => e.Quantity)
                .HasColumnType("smallint")
                .IsRequired();

            builder.Property(e => e.Amount)
                .HasColumnType("decimal(16, 2)")
                .IsRequired();

            builder.Property(e => e.Notes)
                .HasColumnType("ntext");

            builder.Property(e => e.Deleted)
                .HasColumnType("decimal(12, 0)")
                .IsRequired();

            builder.Property(e => e.SelectFlag)
                .HasColumnType("bit")
                .IsRequired();

            builder.Property(e => e.Stamp)
                .HasColumnType("smallint")
                .IsRequired();

            // Relationships
            builder.HasOne(d => d.Job)
                .WithMany()
                .HasForeignKey(d => d.JobID)
                .HasConstraintName("fJobWork_JobID")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(d => d.Employee)
                .WithMany()
                .HasForeignKey(d => d.EmployeeID)
                .HasConstraintName("fJobWork_EmployeeID")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(d => d.WorkType)
                .WithMany()
                .HasForeignKey(d => d.WorkTypeID)
                .HasConstraintName("fJobWork_WorkTypeID")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(d => d.TimeType)
                .WithMany()
                .HasForeignKey(d => d.TimeTypeID)
                .HasConstraintName("fJobWork_TimeTypeID")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
