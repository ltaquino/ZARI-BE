namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over Customer Payments — a generic data browser, not a BIR-compliance
/// report, mirroring SalesInvoicesReportDataset's own pattern. CustomerPayment has no stored header
/// total; like CustomerPaymentMapper's own ToResponse projection, Amount is the sum of each line's
/// AmountApplied (how much of this payment is allocated against each Sales Invoice), computed in
/// memory after materializing (Filterable/Sortable = false) rather than pushed into the SQL
/// predicate/order-by, same "bounded, occasional-use report" precedent as SalesInvoicesReportDataset's
/// own Amount.
/// </summary>
public sealed class CustomerPaymentsReportDataset : IReportDataset
{
    public string Key => "CUSTOMER_PAYMENTS";
    public string Label => "Customer Payments";
    public string RequiredPermissionCode => "CUSTOMER_PAYMENTS";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("PaymentNo", "Payment No.", ReportFieldType.Text),
        new("PaymentDate", "Payment Date", ReportFieldType.Date),
        new("CustomerName", "Customer", ReportFieldType.Text),
        new("BranchId", "Branch", ReportFieldType.Text),
        new("Status", "Status", ReportFieldType.Text),
        new("Amount", "Amount", ReportFieldType.Currency, Filterable: false, Sortable: false),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<CustomerPayment> query = dbContext.CustomerPayments.AsNoTracking()
            .Include(p => p.Customer)
            .Include(p => p.Lines);

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "PaymentNo" => ReportDatasetFilters.Text(query, filter, p => p.PaymentNo),
                "PaymentDate" => ReportDatasetFilters.Date(query, filter, p => (DateTimeOffset?)p.PaymentDate),
                "CustomerName" => ReportDatasetFilters.Text(query, filter, p => p.Customer.Name),
                "BranchId" => ReportDatasetFilters.Text(query, filter, p => p.BranchId),
                "Status" => ReportDatasetFilters.Text(query, filter, p => p.Status),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "PaymentNo" => ReportDatasetFilters.Sort(query, request.SortDescending, p => p.PaymentNo),
            "PaymentDate" => ReportDatasetFilters.Sort(query, request.SortDescending, p => p.PaymentDate),
            "CustomerName" => ReportDatasetFilters.Sort(query, request.SortDescending, p => p.Customer.Name),
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

    private static IReadOnlyDictionary<string, object?> BuildRow(CustomerPayment payment, IReadOnlyList<string> columnKeys)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "PaymentNo" => payment.PaymentNo,
                "PaymentDate" => payment.PaymentDate,
                "CustomerName" => payment.Customer.Name,
                "BranchId" => payment.BranchId,
                "Status" => payment.Status,
                "Amount" => ComputeAmount(payment),
                _ => null,
            };
        }
        return row;
    }

    private static decimal ComputeAmount(CustomerPayment payment) =>
        Math.Round(payment.Lines.Sum(l => l.AmountApplied), 2);
}
