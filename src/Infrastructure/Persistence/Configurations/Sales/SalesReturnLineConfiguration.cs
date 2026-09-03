using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class SalesReturnLineConfiguration : BaseModelConfig, IEntityTypeConfiguration<SalesReturnLine>
{
    public void Configure(EntityTypeBuilder<SalesReturnLine> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.QtyReturned).HasColumnType(DefaultDecimal);
        builder.Property(l => l.UnitPrice).HasColumnType(DefaultDecimal);
        builder.Property(l => l.SerialNo).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasOne(l => l.Item)
            .WithMany()
            .HasForeignKey(l => l.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Uom)
            .WithMany()
            .HasForeignKey(l => l.UomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.DeliveryOrderLine)
            .WithMany()
            .HasForeignKey(l => l.DeliveryOrderLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
