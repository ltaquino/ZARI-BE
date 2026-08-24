using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class StockTransferRequestLineConfiguration : BaseModelConfig, IEntityTypeConfiguration<StockTransferRequestLine>
{
    public void Configure(EntityTypeBuilder<StockTransferRequestLine> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.QtyRequested).HasColumnType(DefaultDecimal);

        builder.HasOne(l => l.Item)
            .WithMany()
            .HasForeignKey(l => l.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Uom)
            .WithMany()
            .HasForeignKey(l => l.UomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
