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
    DbSet<GoodsIssue> GoodsIssues { get; }
    DbSet<GoodsIssueLine> GoodsIssueLines { get; }
    DbSet<StockAdjustment> StockAdjustments { get; }
    DbSet<StockAdjustmentLine> StockAdjustmentLines { get; }
    DbSet<StockOpname> StockOpnames { get; }
    DbSet<StockOpnameLine> StockOpnameLines { get; }
    DbSet<StockTransferRequest> StockTransferRequests { get; }
    DbSet<StockTransferRequestLine> StockTransferRequestLines { get; }
    DbSet<StockLocationTransfer> StockLocationTransfers { get; }
    DbSet<StockLocationTransferLine> StockLocationTransferLines { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Company> Companies { get; }
    DbSet<Branch> Branches { get; }
    DbSet<GlAccount> GlAccounts { get; }
    DbSet<CostCenter> CostCenters { get; }
    DbSet<GlJournal> GlJournals { get; }
    DbSet<ManualJournalEntry> ManualJournalEntries { get; }
    DbSet<ManualJournalEntryLine> ManualJournalEntryLines { get; }
    DbSet<ApprovalRequest> ApprovalRequests { get; }
    DbSet<ApprovalAction> ApprovalActions { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<NotificationRead> NotificationReads { get; }
    DbSet<Form> Forms { get; }
    DbSet<UserBranch> UserBranches { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserFormPermissionOverride> UserFormPermissionOverrides { get; }
    DbSet<Currency> Currencies { get; }
    DbSet<TaxCode> TaxCodes { get; }
    DbSet<FiscalYear> FiscalYears { get; }
    DbSet<ExchangeRate> ExchangeRates { get; }
    DbSet<BankAccount> BankAccounts { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<PurchaseOrder> PurchaseOrders { get; }
    DbSet<PurchaseOrderLine> PurchaseOrderLines { get; }
    DbSet<PurchaseRequest> PurchaseRequests { get; }
    DbSet<PurchaseRequestLine> PurchaseRequestLines { get; }
    DbSet<GoodsReceiptPo> GoodsReceiptPos { get; }
    DbSet<GoodsReceiptPoLine> GoodsReceiptPoLines { get; }
    DbSet<GoodsReturn> GoodsReturns { get; }
    DbSet<GoodsReturnLine> GoodsReturnLines { get; }
    DbSet<ApInvoice> ApInvoices { get; }
    DbSet<ApInvoiceLine> ApInvoiceLines { get; }
    DbSet<ApInvoiceExpenseLine> ApInvoiceExpenseLines { get; }
    DbSet<PurchaseReturnReason> PurchaseReturnReasons { get; }
    DbSet<OutgoingPayment> OutgoingPayments { get; }
    DbSet<OutgoingPaymentLine> OutgoingPaymentLines { get; }
    DbSet<SalesOrder> SalesOrders { get; }
    DbSet<SalesOrderLine> SalesOrderLines { get; }
    DbSet<DeliveryOrder> DeliveryOrders { get; }
    DbSet<DeliveryOrderLine> DeliveryOrderLines { get; }
    DbSet<SalesInvoice> SalesInvoices { get; }
    DbSet<SalesInvoiceLine> SalesInvoiceLines { get; }
    DbSet<CustomerPayment> CustomerPayments { get; }
    DbSet<CustomerPaymentLine> CustomerPaymentLines { get; }
    DbSet<SalesReturn> SalesReturns { get; }
    DbSet<SalesReturnLine> SalesReturnLines { get; }
    DbSet<DiscountRule> DiscountRules { get; }
    DbSet<StatutoryDiscountType> StatutoryDiscountTypes { get; }
    DbSet<ZReading> ZReadings { get; }

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
