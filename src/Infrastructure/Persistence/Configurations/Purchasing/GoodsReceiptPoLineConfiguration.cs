using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class GoodsReceiptPoLineConfiguration : BaseModelConfig, IEntityTypeConfiguration<GoodsReceiptPoLine>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptPoLine> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.BatchNo).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(l => l.SerialNo).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(l => l.QtyReceived).HasColumnType(DefaultDecimal);
        builder.Property(l => l.UnitCost).HasColumnType(DefaultDecimal);

        builder.HasOne(l => l.Item)
            .WithMany()
            .HasForeignKey(l => l.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Uom)
            .WithMany()
            .HasForeignKey(l => l.UomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Location)
            .WithMany()
            .HasForeignKey(l => l.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.PurchaseOrderLine)
            .WithMany()
            .HasForeignKey(l => l.PurchaseOrderLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
