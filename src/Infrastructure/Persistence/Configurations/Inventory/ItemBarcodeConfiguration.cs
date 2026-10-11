using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class ItemBarcodeConfiguration : BaseModelConfig, IEntityTypeConfiguration<ItemBarcode>
{
    public void Configure(EntityTypeBuilder<ItemBarcode> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Barcode).IsRequired().HasMaxLength(50);
        builder.Property(b => b.Description).HasMaxLength(100);
        builder.Property(b => b.PackQty).HasColumnType(DefaultDecimal);

        // A scan must resolve to exactly one item.
        builder.HasIndex(b => b.Barcode).IsUnique();
        builder.HasIndex(b => b.ItemId);

        builder.HasOne(b => b.Item)
            .WithMany()
            .HasForeignKey(b => b.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
