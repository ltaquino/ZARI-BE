using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class GoodsIssueConfiguration : BaseModelConfig, IEntityTypeConfiguration<GoodsIssue>
{
    public void Configure(EntityTypeBuilder<GoodsIssue> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.GiNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(i => i.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(i => i.ReferenceType).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(i => i.DestBranchId).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(i => i.ReasonCode).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(i => i.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(i => i.ShipmentStatus).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(i => i.Remarks).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(i => i.StockTransferRequestRefNo).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(i => i.StockTransferRequestId).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(i => i.CancelledBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(i => i.CancelReason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.HasIndex(i => i.GiNo).IsUnique();
        builder.HasIndex(i => new { i.BranchId, i.GiDate });
        builder.HasIndex(i => i.Status);

        // approveGoodsIssue/approveGoodsIssueCancellation look up the pending approval request for
        // this document by (EntityType, EntityId) — see ApprovalRequestConfiguration's matching
        // index — so no extra index is needed here for that lookup.

        builder.HasOne(i => i.Warehouse)
            .WithMany()
            .HasForeignKey(i => i.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.DestWarehouse)
            .WithMany()
            .HasForeignKey(i => i.DestWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Branch)
            .WithMany()
            .HasForeignKey(i => i.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.DestBranch)
            .WithMany()
            .HasForeignKey(i => i.DestBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.CostCenter)
            .WithMany()
            .HasForeignKey(i => i.CostCenterId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasMany(i => i.Lines)
            .WithOne(l => l.GoodsIssue)
            .HasForeignKey(l => l.GoodsIssueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
