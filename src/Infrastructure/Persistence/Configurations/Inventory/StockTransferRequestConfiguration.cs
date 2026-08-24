using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class StockTransferRequestConfiguration : BaseModelConfig, IEntityTypeConfiguration<StockTransferRequest>
{
    public void Configure(EntityTypeBuilder<StockTransferRequest> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RequestNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.SourceBranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.DestBranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.Remarks).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(r => r.DeclinedBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(r => r.DeclineReason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(r => r.CancelledBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(r => r.CancelReason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.HasIndex(r => r.RequestNo).IsUnique();

        // approveStockTransferRequest/rejectStockTransferRequest look up the pending approval
        // request for this document by (EntityType, EntityId) — see ApprovalRequestConfiguration's
        // matching index — so no extra index is needed here for that lookup.

        builder.HasOne(r => r.SourceWarehouse)
            .WithMany()
            .HasForeignKey(r => r.SourceWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.DestWarehouse)
            .WithMany()
            .HasForeignKey(r => r.DestWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.SourceBranch)
            .WithMany()
            .HasForeignKey(r => r.SourceBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.DestBranch)
            .WithMany()
            .HasForeignKey(r => r.DestBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Lines)
            .WithOne(l => l.StockTransferRequest)
            .HasForeignKey(l => l.StockTransferRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
