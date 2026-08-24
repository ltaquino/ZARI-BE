using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class StockLocationTransferConfiguration : BaseModelConfig, IEntityTypeConfiguration<StockLocationTransfer>
{
    public void Configure(EntityTypeBuilder<StockLocationTransfer> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TransferNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(t => t.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(t => t.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(t => t.Remarks).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(t => t.PostedBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(t => t.CancelledBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(t => t.CancelReason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.HasIndex(t => t.TransferNo).IsUnique();

        builder.HasOne(t => t.Warehouse)
            .WithMany()
            .HasForeignKey(t => t.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Branch)
            .WithMany()
            .HasForeignKey(t => t.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Lines)
            .WithOne(l => l.StockLocationTransfer)
            .HasForeignKey(l => l.StockLocationTransferId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
