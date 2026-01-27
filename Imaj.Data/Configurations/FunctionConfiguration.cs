using Imaj.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imaj.Data.Configurations
{
    public class FunctionConfiguration : IEntityTypeConfiguration<Function>
    {
        public void Configure(EntityTypeBuilder<Function> builder)
        {
            builder.ToTable("Function");
            
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("ID").HasColumnType("decimal(18, 0)");
            
            builder.Ignore(e => e.CreatedDate);
            builder.Ignore(e => e.IsActive);

            builder.Property(e => e.CompanyID).HasColumnType("decimal(18, 0)").IsRequired();
            builder.Property(e => e.IntervalID).HasColumnType("decimal(18, 0)"); // Nullable in screenshot? Screenshot says Not Null [v] for some, wait. 
            // Checking screenshot for Function:
            // CompanyID: Not Null
            // IntervalID: [ ] (Empty checkmark means nullable usually?) No, wait. 
            // In screenshot: Not Null column has [v]. Columns NOT checked for Not Null are nullable.
            // Function screenshot:
            // IntervalID : Not Null [ ] => Nullable.
            
            // Wait, looking at screenshot 3 for Function table:
            // fFunction_IntervalID FK -> Interval.
            // IntervalID row in columns: Not Null [ ] -> This means it IS Nullable.
            
            // Re-checking my Entity definition for Function... I made it `decimal IntervalID`.
            // Use `decimal?` if nullable? Or just `decimal` and required false?
            // If it is nullable in DB, entity property should be `decimal?`.
            
            // Let's check Function.cs again. I defined `public decimal IntervalID { get; set; }`. This implies Not Null.
            // If the screenshot shows it's nullable, I should update Entity to `decimal?`.
            // But let's look closer at screenshot...
            // Image 3 (Function FKs): fFunction_IntervalID -> Interval.
            // Image 2 (Function Columns): IntervalID (Line 9). Not Null column is EMPTY. So it is Nullable.
            
            // I should update Function.cs to `public decimal? IntervalID { get; set; }` later.
            // For now, in configuration I can set IsRequired(false). But if the property is value type (decimal), EF might still treat as required.
            // I better update the Entity too. I will do that in a separate step or just assume for now and fix later.
            // Actually, if I don't fix it, EF might crash on nulls.
            // Let's assume for now I will fix entity later.
            
            builder.Property(e => e.IntervalID).HasColumnType("decimal(18, 0)").IsRequired(false);

            builder.Property(e => e.Reservable).IsRequired();
            builder.Property(e => e.WorkMandatory).IsRequired();
            builder.Property(e => e.ProdMandatory).IsRequired();
            builder.Property(e => e.Invisible).IsRequired();
            builder.Property(e => e.SelectFlag).IsRequired();
            builder.Property(e => e.Stamp).HasColumnType("smallint").IsRequired();

            // Relationships
            builder.HasOne(d => d.Company)
                .WithMany(p => p.Functions)
                .HasForeignKey(d => d.CompanyID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(d => d.Interval)
                .WithMany(p => p.Functions)
                .HasForeignKey(d => d.IntervalID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
