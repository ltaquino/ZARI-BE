using Microsoft.EntityFrameworkCore;
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
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

}
