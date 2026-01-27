using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class UserEmpConfiguration : IEntityTypeConfiguration<UserEmp>
    {
        public void Configure(EntityTypeBuilder<UserEmp> builder)
        {
            builder.ToTable("UserEmp");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(8, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.UserID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.EmployeeID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.Deleted).HasColumnType("decimal(8, 0)").IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.Employee)
                .WithMany()
                .HasForeignKey(d => d.EmployeeID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
