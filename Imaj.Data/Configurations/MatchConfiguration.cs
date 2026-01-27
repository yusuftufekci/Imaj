using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class MatchConfiguration : IEntityTypeConfiguration<Match>
    {
        public void Configure(EntityTypeBuilder<Match> builder)
        {
            builder.ToTable("Match");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(16, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.ReserveID).HasColumnType("decimal(10, 0)").IsRequired();
            builder.Property(e => e.ResourceID).HasColumnType("decimal(8, 0)").IsRequired();
            builder.Property(e => e.AllocateID).HasColumnType("decimal(12, 0)").IsRequired();
            builder.Property(e => e.FunctionID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.AtomicDT).HasColumnType("smalldatetime").IsRequired();
            builder.Property(e => e.Deleted).HasColumnType("decimal(16, 0)").IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.Reserve)
                .WithMany()
                .HasForeignKey(d => d.ReserveID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.Resource)
                .WithMany()
                .HasForeignKey(d => d.ResourceID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.Allocate)
                .WithMany()
                .HasForeignKey(d => d.AllocateID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.Function)
                .WithMany()
                .HasForeignKey(d => d.FunctionID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
