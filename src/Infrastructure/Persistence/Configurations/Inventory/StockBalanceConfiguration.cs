using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class StockBalanceConfiguration : BaseModelConfig, IEntityTypeConfiguration<StockBalance>
{
    public void Configure(EntityTypeBuilder<StockBalance> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.BranchId)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(b => b.BatchNo)
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(b => b.QtyOnHand).HasColumnType(DefaultDecimal);
        builder.Property(b => b.AvgUnitCost).HasColumnType(DefaultDecimal);
        builder.Property(b => b.TotalValue).HasColumnType(DefaultDecimal);

        // Not a true uniqueness guarantee under MySQL's NULL semantics (each NULL BatchNo is
        // distinct to a unique index) — the real correctness guard is the transactional gap lock
        // taken via "SELECT ... WHERE ItemId = ? AND WarehouseId = ? FOR UPDATE" in
        // StockBalanceLocker before any row for that pair is read or created. This index exists so
        // that lock scan is efficient and named-batch duplicates are still caught at the DB level.
        builder.HasIndex(b => new { b.ItemId, b.WarehouseId, b.BatchNo }).IsUnique();

        builder.HasOne(b => b.Item)
            .WithMany()
            .HasForeignKey(b => b.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Warehouse)
            .WithMany()
            .HasForeignKey(b => b.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
