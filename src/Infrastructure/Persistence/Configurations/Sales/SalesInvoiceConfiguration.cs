using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class SalesInvoiceConfiguration : BaseModelConfig, IEntityTypeConfiguration<SalesInvoice>
{
    public void Configure(EntityTypeBuilder<SalesInvoice> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(i => i.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(i => i.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(i => i.Remarks).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(i => i.DiscountPct).HasColumnType(DefaultDecimal);
        builder.Property(i => i.BirOrSeriesNumber).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(i => i.PaidAmount).HasColumnType(DefaultDecimal);
        builder.Property(i => i.CancelledBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(i => i.CancelReason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.HasIndex(i => i.InvoiceNo).IsUnique();
        builder.HasIndex(i => i.BirOrSeriesNumber).IsUnique();

        builder.HasOne(i => i.Branch)
            .WithMany()
            .HasForeignKey(i => i.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Customer)
            .WithMany()
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.DeliveryOrder)
            .WithMany()
            .HasForeignKey(i => i.DeliveryOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(i => i.CostCenter)
            .WithMany()
            .HasForeignKey(i => i.CostCenterId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(i => i.PosTerminal)
            .WithMany()
            .HasForeignKey(i => i.PosTerminalId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasMany(i => i.Lines)
            .WithOne(l => l.SalesInvoice)
            .HasForeignKey(l => l.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
