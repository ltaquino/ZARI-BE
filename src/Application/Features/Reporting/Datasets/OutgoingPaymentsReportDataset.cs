namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over Outgoing Payments. Field shape confirmed against
/// GetCashOutRegisterReportQueryHandler (the existing Cash-Out Register report, the other
/// consumer of this entity): OutgoingPayment has no TotalAmount-equivalent property, so Amount is
/// always the sum of its lines — computed in memory after materializing/capping (same "bounded,
/// occasional-use report" precedent as GetInventoryAsOfQueryHandler / ApInvoicesReportDataset), so
/// it's a display-only column (Filterable/Sortable = false) rather than pushed into the SQL
/// predicate/order-by.
/// </summary>
public sealed class OutgoingPaymentsReportDataset : IReportDataset
{
    public string Key => "OUTGOING_PAYMENTS";
    public string Label => "Outgoing Payments";
    public string RequiredPermissionCode => "OUTGOING_PAYMENTS";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("PaymentNo", "Payment No.", ReportFieldType.Text),
        new("PaymentDate", "Payment Date", ReportFieldType.Date),
        new("SupplierName", "Supplier", ReportFieldType.Text),
        new("BankAccountName", "Bank Account", ReportFieldType.Text),
        new("RefNo", "Ref No.", ReportFieldType.Text),
        new("BranchId", "Branch", ReportFieldType.Text),
        new("Status", "Status", ReportFieldType.Text),
        new("Amount", "Amount", ReportFieldType.Currency, Filterable: false, Sortable: false),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<OutgoingPayment> query = dbContext.OutgoingPayments.AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.BankAccount)
            .Include(p => p.Lines);

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "PaymentNo" => ReportDatasetFilters.Text(query, filter, p => p.PaymentNo),
                "PaymentDate" => ReportDatasetFilters.Date(query, filter, p => (DateTimeOffset?)p.PaymentDate),
                "SupplierName" => ReportDatasetFilters.Text(query, filter, p => p.Supplier.Name),
                "BankAccountName" => ReportDatasetFilters.Text(query, filter, p => p.BankAccount.AccountName),
                "RefNo" => ReportDatasetFilters.Text(query, filter, p => p.RefNo),
                "BranchId" => ReportDatasetFilters.Text(query, filter, p => p.BranchId),
                "Status" => ReportDatasetFilters.Text(query, filter, p => p.Status),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "PaymentNo" => ReportDatasetFilters.Sort(query, request.SortDescending, p => p.PaymentNo),
            "PaymentDate" => ReportDatasetFilters.Sort(query, request.SortDescending, p => p.PaymentDate),
            "SupplierName" => ReportDatasetFilters.Sort(query, request.SortDescending, p => p.Supplier.Name),
            "BankAccountName" => ReportDatasetFilters.Sort(query, request.SortDescending, p => p.BankAccount.AccountName),
            "RefNo" => ReportDatasetFilters.Sort(query, request.SortDescending, p => p.RefNo),
            "BranchId" => ReportDatasetFilters.Sort(query, request.SortDescending, p => p.BranchId),
            "Status" => ReportDatasetFilters.Sort(query, request.SortDescending, p => p.Status),
            _ => query.OrderByDescending(p => p.PaymentDate),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var payments = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var rows = payments.Select(p => BuildRow(p, request.ColumnKeys)).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(OutgoingPayment payment, IReadOnlyList<string> columnKeys)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "PaymentNo" => payment.PaymentNo,
                "PaymentDate" => payment.PaymentDate,
                "SupplierName" => payment.Supplier.Name,
                "BankAccountName" => payment.BankAccount.AccountName,
                "RefNo" => payment.RefNo,
                "BranchId" => payment.BranchId,
                "Status" => payment.Status,
                "Amount" => Math.Round(payment.Lines.Sum(l => l.Amount), 2),
                _ => null,
            };
        }
        return row;
    }
}
