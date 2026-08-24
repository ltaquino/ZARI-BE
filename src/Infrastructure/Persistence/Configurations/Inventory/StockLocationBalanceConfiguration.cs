using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class StockLocationBalanceConfiguration : BaseModelConfig, IEntityTypeConfiguration<StockLocationBalance>
{
    public void Configure(EntityTypeBuilder<StockLocationBalance> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.BatchNo).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(b => b.QtyOnHand).HasColumnType(DefaultDecimal);

        // Same NULL-semantics caveat as StockBalance — the real correctness guard is the
        // transactional gap lock in StockLocationBalanceLocker, not this index alone.
        builder.HasIndex(b => new { b.ItemId, b.WarehouseId, b.LocationId, b.BatchNo }).IsUnique();

        builder.HasOne(b => b.Item)
            .WithMany()
            .HasForeignKey(b => b.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Warehouse)
            .WithMany()
            .HasForeignKey(b => b.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Location)
            .WithMany()
            .HasForeignKey(b => b.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
