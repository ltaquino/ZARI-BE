using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class SalesOrderConfiguration : BaseModelConfig, IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.SoNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(o => o.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(o => o.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(o => o.Remarks).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(o => o.DiscountPct).HasColumnType(DefaultDecimal);
        builder.Property(o => o.CancelledBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(o => o.CancelReason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.HasIndex(o => o.SoNo).IsUnique();
        builder.HasIndex(o => new { o.BranchId, o.OrderDate });
        builder.HasIndex(o => o.Status);

        builder.HasOne(o => o.Branch)
            .WithMany()
            .HasForeignKey(o => o.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Customer)
            .WithMany()
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Lines)
            .WithOne(l => l.SalesOrder)
            .HasForeignKey(l => l.SalesOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
