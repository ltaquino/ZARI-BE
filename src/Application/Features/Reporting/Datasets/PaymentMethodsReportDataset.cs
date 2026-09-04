namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over the Payment Method master data (admin-configurable tender types).
/// GlAccountName is read off the GlAccount navigation, requiring one .Include(), same pattern as
/// GlJournalLinesReportDataset's AccountName. Default sort is Code ascending.
/// </summary>
public sealed class PaymentMethodsReportDataset : IReportDataset
{
    public string Key => "PAYMENT_METHODS";
    public string Label => "Payment Methods";
    public string RequiredPermissionCode => "PAYMENT_METHODS";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("Code", "Code", ReportFieldType.Text),
        new("Name", "Name", ReportFieldType.Text),
        new("GlAccountName", "GL Account", ReportFieldType.Text),
        new("Status", "Status", ReportFieldType.Text),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<PaymentMethod> query = dbContext.PaymentMethods.AsNoTracking()
            .Include(p => p.GlAccount);

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "Code" => ReportDatasetFilters.Text(query, filter, p => p.Code),
                "Name" => ReportDatasetFilters.Text(query, filter, p => p.Name),
                "GlAccountName" => ReportDatasetFilters.Text(query, filter, p => p.GlAccount.Name),
                "Status" => ReportDatasetFilters.Text(query, filter, p => p.Status),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "Code" => ReportDatasetFilters.Sort(query, request.SortDescending, p => p.Code),
            "Name" => ReportDatasetFilters.Sort(query, request.SortDescending, p => p.Name),
            "GlAccountName" => ReportDatasetFilters.Sort(query, request.SortDescending, p => p.GlAccount.Name),
            "Status" => ReportDatasetFilters.Sort(query, request.SortDescending, p => p.Status),
            _ => query.OrderBy(p => p.Code),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var paymentMethods = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var rows = paymentMethods.Select(p => BuildRow(p, request.ColumnKeys)).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(PaymentMethod paymentMethod, IReadOnlyList<string> columnKeys)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "Code" => paymentMethod.Code,
                "Name" => paymentMethod.Name,
                "GlAccountName" => paymentMethod.GlAccount.Name,
                "Status" => paymentMethod.Status,
                _ => null,
            };
        }
        return row;
    }
}
