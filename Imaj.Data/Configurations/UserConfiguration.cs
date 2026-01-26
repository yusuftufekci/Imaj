using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    /// <summary>
    /// User entity için EF Core konfigürasyonu.
    /// </summary>
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // Tablo adı
            builder.ToTable("Users");
            
            // Primary Key
            builder.HasKey(e => e.Id);
            
            // Property konfigürasyonları
            builder.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(50);
            
            builder.Property(e => e.Email)
                .HasMaxLength(100);
            
            builder.Property(e => e.FullName)
                .HasMaxLength(100);
            
            builder.Property(e => e.Role)
                .HasMaxLength(50);
            
            builder.Property(e => e.PasswordHash)
                .HasMaxLength(256);
        }
    }
}
