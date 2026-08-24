using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class GoodsReceiptConfiguration : BaseModelConfig, IEntityTypeConfiguration<GoodsReceipt>
{
    public void Configure(EntityTypeBuilder<GoodsReceipt> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.GrNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.ReceiptType).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.ReceivedBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(r => r.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.Remarks).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(r => r.GoodsIssueRefNo).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.GoodsIssueId).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(r => r.ReasonCode).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.CancelledBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(r => r.CancelReason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.HasIndex(r => r.GrNo).IsUnique();

        // approveGoodsReceipt/approveGoodsReceiptCancellation look up the pending approval request
        // for this document by (EntityType, EntityId) — see ApprovalRequestConfiguration's matching
        // index — so no extra index is needed here for that lookup.

        builder.HasOne(r => r.Warehouse)
            .WithMany()
            .HasForeignKey(r => r.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Lines)
            .WithOne(l => l.GoodsReceipt)
            .HasForeignKey(l => l.GoodsReceiptId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
