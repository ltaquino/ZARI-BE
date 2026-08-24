using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class CostLayerConfiguration : BaseModelConfig, IEntityTypeConfiguration<CostLayer>
{
    public void Configure(EntityTypeBuilder<CostLayer> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.BatchNo)
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(l => l.SourceReferenceTable)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(l => l.SourceReferenceId)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(l => l.QtyReceived).HasColumnType(DefaultDecimal);
        builder.Property(l => l.QtyRemaining).HasColumnType(DefaultDecimal);
        builder.Property(l => l.UnitCost).HasColumnType(DefaultDecimal);

        builder.HasIndex(l => new { l.ItemId, l.WarehouseId, l.BatchNo, l.ReceiptDate });

        builder.HasOne(l => l.Item)
            .WithMany()
            .HasForeignKey(l => l.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Warehouse)
            .WithMany()
            .HasForeignKey(l => l.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
