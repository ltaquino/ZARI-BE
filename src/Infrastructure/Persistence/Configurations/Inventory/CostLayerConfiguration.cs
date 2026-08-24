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

        // Long enough for a real GUID-string line id (36 chars), not just the FE mock's old
        // short-form ids (e.g. "line-1735000000000").
        builder.Property(l => l.SourceReferenceId)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);

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
