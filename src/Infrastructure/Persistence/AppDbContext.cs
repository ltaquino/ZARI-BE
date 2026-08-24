namespace ZARI.Infrastructure.Persistence;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options), IAppDbContext
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();
    public DbSet<Uom> Uoms => Set<Uom>();
    public DbSet<ItemCategory> ItemCategories => Set<ItemCategory>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<AdjustmentReason> AdjustmentReasons => Set<AdjustmentReason>();
    public DbSet<ItemBranchSetting> ItemBranchSettings => Set<ItemBranchSetting>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();
    public DbSet<DocumentSequence> DocumentSequences => Set<DocumentSequence>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<CostLayer> CostLayers => Set<CostLayer>();
    public DbSet<StockLedger> StockLedgers => Set<StockLedger>();
    public DbSet<SerialNumber> SerialNumbers => Set<SerialNumber>();
    public DbSet<StockLocationBalance> StockLocationBalances => Set<StockLocationBalance>();
    public DbSet<GlAccount> GlAccounts => Set<GlAccount>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<GlJournal> GlJournals => Set<GlJournal>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<ApprovalAction> ApprovalActions => Set<ApprovalAction>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationRead> NotificationReads => Set<NotificationRead>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditableEntities()
    {
        var entries = ChangeTracker.Entries<AuditableEntity>();
        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModifiedAt = DateTimeOffset.UtcNow;
                    break;
            }
        }
    }
}
