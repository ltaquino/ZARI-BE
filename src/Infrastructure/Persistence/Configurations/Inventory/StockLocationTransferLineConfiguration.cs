using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class StockLocationTransferLineConfiguration : BaseModelConfig, IEntityTypeConfiguration<StockLocationTransferLine>
{
    public void Configure(EntityTypeBuilder<StockLocationTransferLine> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.BatchNo).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(l => l.SerialNo).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(l => l.Qty).HasColumnType(DefaultDecimal);

        builder.HasOne(l => l.Item)
            .WithMany()
            .HasForeignKey(l => l.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.FromLocation)
            .WithMany()
            .HasForeignKey(l => l.FromLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.ToLocation)
            .WithMany()
            .HasForeignKey(l => l.ToLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
