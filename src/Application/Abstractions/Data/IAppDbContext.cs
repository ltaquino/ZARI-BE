using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ZARI.Domain.Entities;

namespace ZARI.Application.Abstractions.Data;

public interface IAppDbContext
{
    DbSet<TodoItem> Todos { get; }
    DbSet<Uom> Uoms { get; }
    DbSet<ItemCategory> ItemCategories { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<StorageLocation> StorageLocations { get; }
    DbSet<Item> Items { get; }
    DbSet<AdjustmentReason> AdjustmentReasons { get; }
    DbSet<ItemBranchSetting> ItemBranchSettings { get; }
    DbSet<StockReservation> StockReservations { get; }
    DbSet<DocumentSequence> DocumentSequences { get; }
    DbSet<StockBalance> StockBalances { get; }
    DbSet<CostLayer> CostLayers { get; }
    DbSet<StockLedger> StockLedgers { get; }
    DbSet<SerialNumber> SerialNumbers { get; }
    DbSet<StockLocationBalance> StockLocationBalances { get; }
    DbSet<GoodsReceipt> GoodsReceipts { get; }
    DbSet<GoodsReceiptLine> GoodsReceiptLines { get; }
    DbSet<GlAccount> GlAccounts { get; }
    DbSet<CostCenter> CostCenters { get; }
    DbSet<GlJournal> GlJournals { get; }
    DbSet<ApprovalRequest> ApprovalRequests { get; }
    DbSet<ApprovalAction> ApprovalActions { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<NotificationRead> NotificationReads { get; }

    // Exposed only for the stock-ledger posting handlers, which need an explicit transaction plus
    // raw-SQL "FOR UPDATE" locking (see Application/Features/Inventory/StockLedger/Shared/
    // StockBalanceLocker.cs) — ordinary CRUD handlers should never need this.
    DatabaseFacade Database { get; }

    // Same handlers call ChangeTracker.Clear() at the start of each CreateExecutionStrategy()
    // retry attempt, so a retry re-fetches everything fresh instead of colliding with entities
    // still tracked (but never saved) from the attempt that just failed.
    ChangeTracker ChangeTracker { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

}
