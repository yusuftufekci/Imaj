using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class FuncProdConfiguration : IEntityTypeConfiguration<FuncProd>
    {
        public void Configure(EntityTypeBuilder<FuncProd> builder)
        {
            builder.ToTable("FuncProd");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(8, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.FunctionID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.ProductID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.Deleted).HasColumnType("decimal(8, 0)").IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.Function)
                .WithMany()
                .HasForeignKey(d => d.FunctionID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.Product)
                .WithMany()
                .HasForeignKey(d => d.ProductID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
