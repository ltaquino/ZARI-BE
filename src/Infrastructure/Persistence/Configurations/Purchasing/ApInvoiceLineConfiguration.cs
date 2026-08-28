using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class ApInvoiceLineConfiguration : BaseModelConfig, IEntityTypeConfiguration<ApInvoiceLine>
{
    public void Configure(EntityTypeBuilder<ApInvoiceLine> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Qty).HasColumnType(DefaultDecimal);
        builder.Property(l => l.UnitCost).HasColumnType(DefaultDecimal);

        builder.HasOne(l => l.Item)
            .WithMany()
            .HasForeignKey(l => l.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Uom)
            .WithMany()
            .HasForeignKey(l => l.UomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.GoodsReceiptPoLine)
            .WithMany()
            .HasForeignKey(l => l.GoodsReceiptPoLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
