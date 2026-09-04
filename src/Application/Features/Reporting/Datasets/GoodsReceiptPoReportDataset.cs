namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over Goods Receipt (PO) — the entity class is `GoodsReceiptPo`
/// (GRPO), which carries a direct SupplierId/Supplier navigation (a GRPO doesn't require a PO on
/// file, e.g. a walk-in delivery, so Supplier is reached directly rather than via PurchaseOrder).
/// Every field here is a plain scalar column or a simple one-hop navigation join (Supplier.Name),
/// so nothing here needs an in-memory computed column — every field stays fully filterable/sortable
/// at the SQL level.
/// </summary>
public sealed class GoodsReceiptPoReportDataset : IReportDataset
{
    public string Key => "GOODS_RECEIPT_PO";
    public string Label => "Goods Receipt (PO)";
    public string RequiredPermissionCode => "GOODS_RECEIPT_PO";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("GrpoNo", "Receipt No.", ReportFieldType.Text),
        new("ReceiptDate", "Receipt Date", ReportFieldType.Date),
        new("SupplierName", "Supplier", ReportFieldType.Text),
        new("BranchId", "Branch", ReportFieldType.Text),
        new("Status", "Status", ReportFieldType.Text),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<GoodsReceiptPo> query = dbContext.GoodsReceiptPos.AsNoTracking()
            .Include(r => r.Supplier);

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "GrpoNo" => ReportDatasetFilters.Text(query, filter, r => r.GrpoNo),
                "ReceiptDate" => ReportDatasetFilters.Date(query, filter, r => (DateTimeOffset?)r.ReceiptDate),
                "SupplierName" => ReportDatasetFilters.Text(query, filter, r => r.Supplier.Name),
                "BranchId" => ReportDatasetFilters.Text(query, filter, r => r.BranchId),
                "Status" => ReportDatasetFilters.Text(query, filter, r => r.Status),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "GrpoNo" => ReportDatasetFilters.Sort(query, request.SortDescending, r => r.GrpoNo),
            "ReceiptDate" => ReportDatasetFilters.Sort(query, request.SortDescending, r => r.ReceiptDate),
            "SupplierName" => ReportDatasetFilters.Sort(query, request.SortDescending, r => r.Supplier.Name),
            "BranchId" => ReportDatasetFilters.Sort(query, request.SortDescending, r => r.BranchId),
            "Status" => ReportDatasetFilters.Sort(query, request.SortDescending, r => r.Status),
            _ => query.OrderByDescending(r => r.ReceiptDate),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var receipts = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var rows = receipts.Select(r => BuildRow(r, request.ColumnKeys)).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(GoodsReceiptPo receipt, IReadOnlyList<string> columnKeys)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "GrpoNo" => receipt.GrpoNo,
                "ReceiptDate" => receipt.ReceiptDate,
                "SupplierName" => receipt.Supplier.Name,
                "BranchId" => receipt.BranchId,
                "Status" => receipt.Status,
                _ => null,
            };
        }
        return row;
    }
}
