using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class StockOpnameConfiguration : BaseModelConfig, IEntityTypeConfiguration<StockOpname>
{
    public void Configure(EntityTypeBuilder<StockOpname> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.OpnameNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(o => o.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(o => o.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(o => o.Remarks).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(o => o.PostedBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(o => o.CancelledBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(o => o.CancelReason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.HasIndex(o => o.OpnameNo).IsUnique();

        // approveStockOpnameCancellation/rejectStockOpnameCancellation look up the pending
        // cancellation request for this document by (EntityType, EntityId) — see
        // ApprovalRequestConfiguration's matching index — so no extra index is needed here.

        builder.HasOne(o => o.Warehouse)
            .WithMany()
            .HasForeignKey(o => o.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Branch)
            .WithMany()
            .HasForeignKey(o => o.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.CostCenter)
            .WithMany()
            .HasForeignKey(o => o.CostCenterId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasMany(o => o.Lines)
            .WithOne(l => l.StockOpname)
            .HasForeignKey(l => l.StockOpnameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
