using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class SalesInvoiceLineConfiguration : BaseModelConfig, IEntityTypeConfiguration<SalesInvoiceLine>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceLine> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Qty).HasColumnType(DefaultDecimal);
        builder.Property(l => l.UnitPrice).HasColumnType(DefaultDecimal);
        builder.Property(l => l.DiscountPct).HasColumnType(DefaultDecimal);
        builder.Property(l => l.DiscountSourceType).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(l => l.VatType).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(l => l.StatutoryIdNumber).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(l => l.SerialNo).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.HasOne(l => l.Item)
            .WithMany()
            .HasForeignKey(l => l.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Uom)
            .WithMany()
            .HasForeignKey(l => l.UomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.StatutoryDiscountType)
            .WithMany()
            .HasForeignKey(l => l.StatutoryDiscountTypeId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(l => l.DeliveryOrderLine)
            .WithMany()
            .HasForeignKey(l => l.DeliveryOrderLineId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
