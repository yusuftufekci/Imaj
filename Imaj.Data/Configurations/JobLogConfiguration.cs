using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    /// <summary>
    /// JobLog entity'si için EF Core yapılandırması.
    /// Veritabanındaki JobLog tablosuna mapping yapar.
    /// </summary>
    public class JobLogConfiguration : IEntityTypeConfiguration<JobLog>
    {
        public void Configure(EntityTypeBuilder<JobLog> builder)
        {
            builder.ToTable("JobLog");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(12, 0)");
            
            // BaseEntity'den gelen alanları ignore et
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            // JobID kolonu
            builder.Property(e => e.JobID).HasColumnType("decimal(10, 0)").IsRequired();
            
            // Tarih kolonu - veritabanında ActionDT olarak geçiyor
            builder.Property(e => e.ActionDT).HasColumnType("smalldatetime").IsRequired();
            
            // Log action ID
            builder.Property(e => e.LogActionID).HasColumnType("decimal(4, 0)").IsRequired();
            
            // User ID
            builder.Property(e => e.UserID).HasColumnType("decimal(6, 0)").IsRequired();
            
            // Destination kolonu - e-posta adresi vb. için kullanılıyor
            builder.Property(e => e.Destination).HasMaxLength(64).IsRequired();
            
            // Stamp kolonu
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            // İlişkiler
            builder.HasOne(d => d.Job)
                .WithMany()
                .HasForeignKey(d => d.JobID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.LogAction)
                .WithMany()
                .HasForeignKey(d => d.LogActionID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}

