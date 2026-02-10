using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class JobProdCatConfiguration : IEntityTypeConfiguration<JobProdCat>
    {
        public void Configure(EntityTypeBuilder<JobProdCat> builder)
        {
            builder.ToTable("JobProdCat");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(12, 0)");

            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.JobID).HasColumnType("decimal(10, 0)").IsRequired();
            builder.Property(e => e.ProdCatID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.GrossAmount).HasColumnType("decimal(16, 2)").IsRequired();
            builder.Property(e => e.DiscPercentage).HasColumnType("tinyint").IsRequired();
            builder.Property(e => e.DiscAmount).HasColumnType("decimal(16, 2)").IsRequired();
            builder.Property(e => e.NetAmount).HasColumnType("decimal(16, 2)").IsRequired();
            builder.Property(e => e.Deleted).HasColumnType("decimal(12, 0)").IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.Job)
                .WithMany()
                .HasForeignKey(d => d.JobID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.ProdCat)
                .WithMany()
                .HasForeignKey(d => d.ProdCatID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
