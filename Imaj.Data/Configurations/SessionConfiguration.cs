using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class SessionConfiguration : IEntityTypeConfiguration<Session>
    {
        public void Configure(EntityTypeBuilder<Session> builder)
        {
            builder.ToTable("Session");
            builder.HasKey(e => e.Id);

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.SessionID).HasColumnType("uniqueidentifier").IsRequired();
            builder.Property(e => e.UserID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.LanguageID).HasColumnType("decimal(2, 0)").IsRequired();
            builder.Property(e => e.CultureID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.StateID).HasColumnType("decimal(4, 0)").IsRequired();
            builder.Property(e => e.LastAccessDT).HasColumnType("smalldatetime").IsRequired();
            builder.Property(e => e.UserAll).HasColumnType("nvarchar(10)").HasMaxLength(10).IsRequired();
            builder.Property(e => e.UserCont).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.UserMenu).HasColumnType("ntext").IsRequired();
            builder.Property(e => e.Timeout).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();
        }
    }
}
