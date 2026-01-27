using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class FuncResoConfiguration : IEntityTypeConfiguration<FuncReso>
    {
        public void Configure(EntityTypeBuilder<FuncReso> builder)
        {
            builder.ToTable("FuncReso");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(10, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.FuncRuleID).HasColumnType("decimal(8, 0)").IsRequired();
            builder.Property(e => e.ResoCatID).HasColumnType("decimal(6, 0)").IsRequired();
            builder.Property(e => e.Deleted).HasColumnType("decimal(10, 0)").IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            builder.HasOne(d => d.FuncRule)
                .WithMany()
                .HasForeignKey(d => d.FuncRuleID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.ResoCat)
                .WithMany()
                .HasForeignKey(d => d.ResoCatID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
