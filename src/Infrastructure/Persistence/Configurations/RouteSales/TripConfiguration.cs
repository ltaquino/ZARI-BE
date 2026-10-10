using ZARI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZARI.Infrastructure.Persistence.Configurations;

public sealed class TripConfiguration : BaseModelConfig, IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TripNo).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(t => t.BranchId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(t => t.DriverUserId).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(t => t.HelperNames).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(t => t.Status).IsRequired().HasMaxLength(30);
        builder.Property(t => t.Remarks).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(t => t.CashExpected).HasColumnType(DefaultDecimal);
        builder.Property(t => t.CashRemitted).HasColumnType(DefaultDecimal);
        builder.Property(t => t.ShortageCharged).HasColumnType(DefaultDecimal);
        builder.Property(t => t.ShortagePaid).HasColumnType(DefaultDecimal);
        builder.Property(t => t.ShortageAmount).HasColumnType(DefaultDecimal);
        builder.Property(t => t.ShortageSettlement).HasMaxLength(20);
        builder.Property(t => t.CancelledBy).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(t => t.CancelReason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);

        builder.HasIndex(t => t.TripNo).IsUnique();
        builder.HasIndex(t => new { t.BranchId, t.TripDate });
        builder.HasIndex(t => new { t.DriverUserId, t.Status });
        builder.HasIndex(t => new { t.VehicleId, t.Status });

        builder.HasOne(t => t.Branch).WithMany().HasForeignKey(t => t.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Vehicle).WithMany().HasForeignKey(t => t.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Route).WithMany().HasForeignKey(t => t.RouteId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        builder.HasOne(t => t.SourceWarehouse).WithMany().HasForeignKey(t => t.SourceWarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.DriverEmployee).WithMany().HasForeignKey(t => t.DriverEmployeeId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);

        builder.HasMany(t => t.Lines).WithOne(l => l.Trip).HasForeignKey(l => l.TripId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(t => t.SalesOrders).WithOne(s => s.Trip).HasForeignKey(s => s.TripId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TripLineConfiguration : BaseModelConfig, IEntityTypeConfiguration<TripLine>
{
    public void Configure(EntityTypeBuilder<TripLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Ignore(l => l.QtyToLoad);

        foreach (var p in new[] { nameof(TripLine.QtyOrdered), nameof(TripLine.QtyExtra), nameof(TripLine.QtyLoaded), nameof(TripLine.QtyDelivered),
                     nameof(TripLine.QtyVanSold), nameof(TripLine.QtyRefused), nameof(TripLine.QtyReturnedGood), nameof(TripLine.QtyReturnedDamaged), nameof(TripLine.QtyLoadedBack),
                     nameof(TripLine.QtyDamagedOut), nameof(TripLine.QtyVariance) })
            builder.Property(typeof(decimal), p).HasColumnType(DefaultDecimal);
        builder.Property(l => l.QtyLoadCheck).HasColumnType(DefaultDecimal);
        builder.Property(l => l.QtyCountedAtEnd).HasColumnType(DefaultDecimal);

        builder.HasIndex(l => new { l.TripId, l.ItemId }).IsUnique();
        builder.HasOne(l => l.Item).WithMany().HasForeignKey(l => l.ItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TripSalesOrderConfiguration : BaseModelConfig, IEntityTypeConfiguration<TripSalesOrder>
{
    public void Configure(EntityTypeBuilder<TripSalesOrder> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => new { s.TripId, s.SalesOrderId }).IsUnique();
        builder.HasOne(s => s.SalesOrder).WithMany().HasForeignKey(s => s.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TripStockMoveConfiguration : BaseModelConfig, IEntityTypeConfiguration<TripStockMove>
{
    public void Configure(EntityTypeBuilder<TripStockMove> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.MoveType).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(m => m.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.HasIndex(m => new { m.TripId, m.MoveType }).IsUnique();
        builder.HasOne(m => m.Trip).WithMany().HasForeignKey(m => m.TripId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(m => m.Lines).WithOne(l => l.TripStockMove).HasForeignKey(l => l.TripStockMoveId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TripStockMoveLineConfiguration : BaseModelConfig, IEntityTypeConfiguration<TripStockMoveLine>
{
    public void Configure(EntityTypeBuilder<TripStockMoveLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Qty).HasColumnType(DefaultDecimal);
        builder.Property(l => l.UnitCost).HasColumnType(DefaultDecimal);
        builder.HasOne(l => l.Item).WithMany().HasForeignKey(l => l.ItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RouteSaleConfiguration : BaseModelConfig, IEntityTypeConfiguration<RouteSale>
{
    public void Configure(EntityTypeBuilder<RouteSale> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.BirOrSeriesNumber).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.RequestJson).IsRequired().HasColumnType("longtext");
        builder.Property(r => r.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.InvoiceTotal).HasColumnType(DefaultDecimal);
        builder.Property(r => r.AmountPaid).HasColumnType(DefaultDecimal);
        builder.Property(r => r.CashCollected).HasColumnType(DefaultDecimal);
        builder.Property(r => r.VoidReason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(r => r.LastError).HasMaxLength(1000);
        builder.Property(r => r.ReceivedByName).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(r => r.SignatureFilePath).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(r => r.PhotoFilePath).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.Property(r => r.Latitude).HasColumnType("DECIMAL(10,7)");
        builder.Property(r => r.Longitude).HasColumnType("DECIMAL(10,7)");

        // The idempotency key: one record per number a handheld (or booklet) issued.
        builder.HasIndex(r => new { r.PosTerminalId, r.BirOrSeriesNumber }).IsUnique();
        builder.HasIndex(r => new { r.TripId, r.Status });

        builder.HasOne(r => r.Trip).WithMany().HasForeignKey(r => r.TripId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Customer).WithMany().HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
    }
}

public sealed class TripReturnConfiguration : BaseModelConfig, IEntityTypeConfiguration<TripReturn>
{
    public void Configure(EntityTypeBuilder<TripReturn> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(r => r.Reason).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.HasIndex(r => r.ClientRef).IsUnique();
        builder.HasOne(r => r.Trip).WithMany().HasForeignKey(r => r.TripId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Customer).WithMany().HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(r => r.Lines).WithOne(l => l.TripReturn).HasForeignKey(l => l.TripReturnId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TripReturnLineConfiguration : BaseModelConfig, IEntityTypeConfiguration<TripReturnLine>
{
    public void Configure(EntityTypeBuilder<TripReturnLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Qty).HasColumnType(DefaultDecimal);
        builder.Property(l => l.UnitPrice).HasColumnType(DefaultDecimal);
    }
}

public sealed class TripCollectionConfiguration : BaseModelConfig, IEntityTypeConfiguration<TripCollection>
{
    public void Configure(EntityTypeBuilder<TripCollection> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Status).IsRequired().HasMaxLength((int)EnumColumnLength.VARCHARDEFAULT);
        builder.Property(c => c.Amount).HasColumnType(DefaultDecimal);
        builder.Property(c => c.CashAmount).HasColumnType(DefaultDecimal);
        builder.HasIndex(c => c.ClientRef).IsUnique();
        builder.HasOne(c => c.Trip).WithMany().HasForeignKey(c => c.TripId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.Customer).WithMany().HasForeignKey(c => c.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TripRefusalConfiguration : BaseModelConfig, IEntityTypeConfiguration<TripRefusal>
{
    public void Configure(EntityTypeBuilder<TripRefusal> builder)
    {
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => r.ClientRef).IsUnique();
        builder.HasIndex(r => new { r.TripId, r.SalesOrderId });
        builder.HasOne(r => r.Trip).WithMany().HasForeignKey(r => r.TripId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Customer).WithMany().HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.SalesOrder).WithMany().HasForeignKey(r => r.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(r => r.Lines).WithOne(l => l.TripRefusal).HasForeignKey(l => l.TripRefusalId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TripRefusalLineConfiguration : BaseModelConfig, IEntityTypeConfiguration<TripRefusalLine>
{
    public void Configure(EntityTypeBuilder<TripRefusalLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Qty).HasColumnType(DefaultDecimal);
        builder.Property(l => l.ReasonCode).IsRequired().HasMaxLength(30);
        builder.Property(l => l.Notes).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.HasIndex(l => l.SalesOrderLineId);
        builder.HasOne(l => l.Item).WithMany().HasForeignKey(l => l.ItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TripShortagePaymentConfiguration : BaseModelConfig, IEntityTypeConfiguration<TripShortagePayment>
{
    public void Configure(EntityTypeBuilder<TripShortagePayment> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Amount).HasColumnType(DefaultDecimal);
        builder.Property(p => p.ReferenceNo).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_150);
        builder.Property(p => p.Remarks).HasMaxLength((int)EnumColumnLength.VARCHAR_FOR_300);
        builder.HasOne(p => p.Trip).WithMany().HasForeignKey(p => p.TripId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.PaymentMethod).WithMany().HasForeignKey(p => p.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
    }
}

