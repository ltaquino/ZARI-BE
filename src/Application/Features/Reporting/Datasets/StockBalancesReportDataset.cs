namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over the denormalized StockBalance current on-hand snapshot — distinct
/// from the existing STOCK_LEDGER dataset, which is transaction history, not a point-in-time
/// balance. ItemName/ItemCode come via an Item Include (StockBalance itself carries no denormalized
/// item snapshot columns, unlike StockLedger), so a later item rename is reflected immediately —
/// consistent with this row being a live-maintained current balance, not an immutable historical
/// record. No specific FE formCode guards the Stock Balances page (ZARI-FE App.tsx mounts it with
/// no RequireFormView wrapper), so this falls back to gating on "ITEMS" per the task's own fallback
/// rule, same as StockLedgerReportDataset.
/// </summary>
public sealed class StockBalancesReportDataset : IReportDataset
{
    public string Key => "STOCK_BALANCES";
    public string Label => "Stock Balances";
    public string RequiredPermissionCode => "ITEMS";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("ItemName", "Item", ReportFieldType.Text),
        new("ItemCode", "Item Code", ReportFieldType.Text),
        new("WarehouseName", "Warehouse", ReportFieldType.Text),
        new("BranchId", "Branch", ReportFieldType.Text),
        new("QtyOnHand", "Qty On Hand", ReportFieldType.Number),
        new("AvgUnitCost", "Avg Unit Cost", ReportFieldType.Currency),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<StockBalance> query = dbContext.StockBalances.AsNoTracking()
            .Include(b => b.Item)
            .Include(b => b.Warehouse);

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "ItemName" => ReportDatasetFilters.Text(query, filter, b => b.Item.Name),
                "ItemCode" => ReportDatasetFilters.Text(query, filter, b => b.Item.Code),
                "WarehouseName" => ReportDatasetFilters.Text(query, filter, b => b.Warehouse.Name),
                "BranchId" => ReportDatasetFilters.Text(query, filter, b => b.BranchId),
                "QtyOnHand" => ReportDatasetFilters.Decimal(query, filter, b => (decimal?)b.QtyOnHand),
                "AvgUnitCost" => ReportDatasetFilters.Decimal(query, filter, b => (decimal?)b.AvgUnitCost),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "ItemName" => ReportDatasetFilters.Sort(query, request.SortDescending, b => b.Item.Name),
            "ItemCode" => ReportDatasetFilters.Sort(query, request.SortDescending, b => b.Item.Code),
            "WarehouseName" => ReportDatasetFilters.Sort(query, request.SortDescending, b => b.Warehouse.Name),
            "BranchId" => ReportDatasetFilters.Sort(query, request.SortDescending, b => b.BranchId),
            "QtyOnHand" => ReportDatasetFilters.Sort(query, request.SortDescending, b => b.QtyOnHand),
            "AvgUnitCost" => ReportDatasetFilters.Sort(query, request.SortDescending, b => b.AvgUnitCost),
            _ => query.OrderBy(b => b.Item.Name),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var balances = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var rows = balances.Select(b => BuildRow(b, request.ColumnKeys)).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(StockBalance balance, IReadOnlyList<string> columnKeys)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "ItemName" => balance.Item.Name,
                "ItemCode" => balance.Item.Code,
                "WarehouseName" => balance.Warehouse.Name,
                "BranchId" => balance.BranchId,
                "QtyOnHand" => balance.QtyOnHand,
                "AvgUnitCost" => balance.AvgUnitCost,
                _ => null,
            };
        }
        return row;
    }
}
