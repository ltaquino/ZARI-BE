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
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptLine> GoodsReceiptLines => Set<GoodsReceiptLine>();
    public DbSet<GoodsIssue> GoodsIssues => Set<GoodsIssue>();
    public DbSet<GoodsIssueLine> GoodsIssueLines => Set<GoodsIssueLine>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<StockAdjustmentLine> StockAdjustmentLines => Set<StockAdjustmentLine>();
    public DbSet<StockOpname> StockOpnames => Set<StockOpname>();
    public DbSet<StockOpnameLine> StockOpnameLines => Set<StockOpnameLine>();
    public DbSet<StockTransferRequest> StockTransferRequests => Set<StockTransferRequest>();
    public DbSet<StockTransferRequestLine> StockTransferRequestLines => Set<StockTransferRequestLine>();
    public DbSet<StockLocationTransfer> StockLocationTransfers => Set<StockLocationTransfer>();
    public DbSet<StockLocationTransferLine> StockLocationTransferLines => Set<StockLocationTransferLine>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<GlAccount> GlAccounts => Set<GlAccount>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<GlJournal> GlJournals => Set<GlJournal>();
    public DbSet<ManualJournalEntry> ManualJournalEntries => Set<ManualJournalEntry>();
    public DbSet<ManualJournalEntryLine> ManualJournalEntryLines => Set<ManualJournalEntryLine>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<ApprovalAction> ApprovalActions => Set<ApprovalAction>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationRead> NotificationReads => Set<NotificationRead>();
    public DbSet<Form> Forms => Set<Form>();
    public DbSet<UserBranch> UserBranches => Set<UserBranch>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserFormPermissionOverride> UserFormPermissionOverrides => Set<UserFormPermissionOverride>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<TaxCode> TaxCodes => Set<TaxCode>();
    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();
    public DbSet<PurchaseRequestLine> PurchaseRequestLines => Set<PurchaseRequestLine>();
    public DbSet<GoodsReceiptPo> GoodsReceiptPos => Set<GoodsReceiptPo>();
    public DbSet<GoodsReceiptPoLine> GoodsReceiptPoLines => Set<GoodsReceiptPoLine>();
    public DbSet<GoodsReturn> GoodsReturns => Set<GoodsReturn>();
    public DbSet<GoodsReturnLine> GoodsReturnLines => Set<GoodsReturnLine>();
    public DbSet<ApInvoice> ApInvoices => Set<ApInvoice>();
    public DbSet<ApInvoiceLine> ApInvoiceLines => Set<ApInvoiceLine>();
    public DbSet<ApInvoiceExpenseLine> ApInvoiceExpenseLines => Set<ApInvoiceExpenseLine>();
    public DbSet<PurchaseReturnReason> PurchaseReturnReasons => Set<PurchaseReturnReason>();
    public DbSet<OutgoingPayment> OutgoingPayments => Set<OutgoingPayment>();
    public DbSet<OutgoingPaymentLine> OutgoingPaymentLines => Set<OutgoingPaymentLine>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();
    public DbSet<DeliveryOrder> DeliveryOrders => Set<DeliveryOrder>();
    public DbSet<DeliveryOrderLine> DeliveryOrderLines => Set<DeliveryOrderLine>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<SalesInvoiceLine> SalesInvoiceLines => Set<SalesInvoiceLine>();
    public DbSet<CustomerPayment> CustomerPayments => Set<CustomerPayment>();
    public DbSet<CustomerPaymentLine> CustomerPaymentLines => Set<CustomerPaymentLine>();
    public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();
    public DbSet<SalesReturnLine> SalesReturnLines => Set<SalesReturnLine>();
    public DbSet<DiscountRule> DiscountRules => Set<DiscountRule>();
    public DbSet<StatutoryDiscountType> StatutoryDiscountTypes => Set<StatutoryDiscountType>();
    public DbSet<ZReading> ZReadings => Set<ZReading>();

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
