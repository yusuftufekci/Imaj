using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class FuncRuleConfiguration : IEntityTypeConfiguration<FuncRule>
    {
        public void Configure(EntityTypeBuilder<FuncRule> builder)
        {
            builder.ToTable("FuncRule");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(8, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.FunctionID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.Name).HasMaxLength(32).IsRequired();
            builder.Property(e => e.MinValue).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.MaxValue).HasColumnType("smallint").IsRequired();
            builder.Property(e => e.Deleted).HasColumnType("decimal(8, 0)").IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.Function)
                .WithMany()
                .HasForeignKey(d => d.FunctionID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
