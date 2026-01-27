using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("User");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(6, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.LanguageID).HasColumnType("decimal(2, 0)").IsRequired();
            builder.Property(e => e.CompanyID).HasColumnType("decimal(4, 0)");
            builder.Property(e => e.Code).HasMaxLength(16).IsRequired();
            builder.Property(e => e.Name).HasMaxLength(48).IsRequired();
            builder.Property(e => e.Password).HasMaxLength(32).IsRequired();
            builder.Property(e => e.AllEmployee).IsRequired();
            builder.Property(e => e.Invisible).IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.Language)
                .WithMany()
                .HasForeignKey(d => d.LanguageID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
