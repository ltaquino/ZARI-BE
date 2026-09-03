using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class SalesReturnConfiguration : BaseModelConfig, IEntityTypeConfiguration<SalesReturn>
{
    public void Configure(EntityTypeBuilder<SalesReturn> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReturnNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.Remarks).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(r => r.CancelledBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(r => r.CancelReason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.HasIndex(r => r.ReturnNo).IsUnique();
        builder.HasIndex(r => new { r.BranchId, r.ReturnDate });
        builder.HasIndex(r => r.Status);

        builder.HasOne(r => r.Branch)
            .WithMany()
            .HasForeignKey(r => r.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Warehouse)
            .WithMany()
            .HasForeignKey(r => r.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.DeliveryOrder)
            .WithMany()
            .HasForeignKey(r => r.DeliveryOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(r => r.CostCenter)
            .WithMany()
            .HasForeignKey(r => r.CostCenterId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasMany(r => r.Lines)
            .WithOne(l => l.SalesReturn)
            .HasForeignKey(l => l.SalesReturnId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
