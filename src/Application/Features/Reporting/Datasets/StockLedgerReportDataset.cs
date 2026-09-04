namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over the immutable StockLedger movement log. ItemName/ItemCode/UomCode
/// are read straight off StockLedger's own denormalized snapshot columns (not via an Item Include)
/// — same "ultimate immutable record" reasoning StockLedger's own doc comment gives, so a later item
/// rename can't rewrite what an old movement said. Qty is signed net movement (QtyIn - QtyOut), a
/// plain arithmetic difference of two scalar columns so — unlike this dataset's siblings' computed
/// currency columns — it stays fully filterable/sortable at the SQL level. No specific FE formCode
/// guards the Stock Card page (ZARI-FE App.tsx mounts it with no RequireFormView wrapper), so this
/// falls back to gating on "ITEMS" per the task's own fallback rule.
/// </summary>
public sealed class StockLedgerReportDataset : IReportDataset
{
    public string Key => "STOCK_LEDGER";
    public string Label => "Stock Ledger";
    public string RequiredPermissionCode => "ITEMS";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("TransactionDate", "Transaction Date", ReportFieldType.Date),
        new("ItemName", "Item", ReportFieldType.Text),
        new("WarehouseName", "Warehouse", ReportFieldType.Text),
        new("TransactionType", "Transaction Type", ReportFieldType.Text),
        new("Qty", "Qty", ReportFieldType.Number),
        new("UnitCost", "Unit Cost", ReportFieldType.Currency),
        new("BranchId", "Branch", ReportFieldType.Text),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<StockLedger> query = dbContext.StockLedgers.AsNoTracking()
            .Include(l => l.Warehouse);

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "TransactionDate" => ReportDatasetFilters.Date(query, filter, l => (DateTimeOffset?)l.TransactionDate),
                "ItemName" => ReportDatasetFilters.Text(query, filter, l => l.ItemName),
                "WarehouseName" => ReportDatasetFilters.Text(query, filter, l => l.Warehouse.Name),
                "TransactionType" => ReportDatasetFilters.Text(query, filter, l => l.TransactionType),
                "Qty" => ReportDatasetFilters.Decimal(query, filter, l => (decimal?)(l.QtyIn - l.QtyOut)),
                "UnitCost" => ReportDatasetFilters.Decimal(query, filter, l => (decimal?)l.UnitCost),
                "BranchId" => ReportDatasetFilters.Text(query, filter, l => l.BranchId),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "TransactionDate" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.TransactionDate),
            "ItemName" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.ItemName),
            "WarehouseName" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.Warehouse.Name),
            "TransactionType" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.TransactionType),
            "Qty" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.QtyIn - l.QtyOut),
            "UnitCost" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.UnitCost),
            "BranchId" => ReportDatasetFilters.Sort(query, request.SortDescending, l => l.BranchId),
            _ => query.OrderByDescending(l => l.TransactionDate),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var entries = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var rows = entries.Select(l => BuildRow(l, request.ColumnKeys)).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(StockLedger entry, IReadOnlyList<string> columnKeys)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "TransactionDate" => entry.TransactionDate,
                "ItemName" => entry.ItemName,
                "WarehouseName" => entry.Warehouse.Name,
                "TransactionType" => entry.TransactionType,
                "Qty" => entry.QtyIn - entry.QtyOut,
                "UnitCost" => entry.UnitCost,
                "BranchId" => entry.BranchId,
                _ => null,
            };
        }
        return row;
    }
}
