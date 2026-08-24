using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class StockLedgerConfiguration : BaseModelConfig, IEntityTypeConfiguration<StockLedger>
{
    public void Configure(EntityTypeBuilder<StockLedger> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ItemCode).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_ITEMCODE);
        builder.Property(l => l.ItemName).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(l => l.UomCode).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_UOMCODE);

        builder.Property(l => l.BranchId)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(l => l.BatchNo).HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(l => l.TransactionType)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        builder.Property(l => l.ReferenceTable)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);

        // Long enough for a real GUID-string line id (36 chars), not just the FE mock's old
        // short-form ids (e.g. "line-1735000000000").
        builder.Property(l => l.ReferenceId)
            .IsRequired()
            .HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);

        builder.Property(l => l.QtyIn).HasColumnType(DefaultDecimal);
        builder.Property(l => l.QtyOut).HasColumnType(DefaultDecimal);
        builder.Property(l => l.UnitCost).HasColumnType(DefaultDecimal);
        builder.Property(l => l.RunningBalanceQty).HasColumnType(DefaultDecimal);
        builder.Property(l => l.RunningBalanceValue).HasColumnType(DefaultDecimal);

        builder.Property(l => l.ConsumptionsJson).HasColumnType(DefaultTextMedium);
        builder.Property(l => l.BalanceDrawsJson).HasColumnType(DefaultTextMedium);

        builder.HasIndex(l => new { l.ItemId, l.WarehouseId, l.BatchNo, l.PostedAt });
        builder.HasIndex(l => new { l.ReferenceTable, l.ReferenceId });

        builder.HasOne(l => l.Item)
            .WithMany()
            .HasForeignKey(l => l.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Warehouse)
            .WithMany()
            .HasForeignKey(l => l.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Branch)
            .WithMany()
            .HasForeignKey(l => l.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
