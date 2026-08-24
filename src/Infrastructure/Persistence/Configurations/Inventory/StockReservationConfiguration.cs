using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class StockReservationConfiguration : BaseModelConfig, IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.BranchId)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(r => r.QtyReserved).HasColumnType(DefaultDecimal);

        builder.Property(r => r.ReferenceNote).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(r => r.ReleasedBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);

        builder.HasOne(r => r.Item)
            .WithMany()
            .HasForeignKey(r => r.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Warehouse)
            .WithMany()
            .HasForeignKey(r => r.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
