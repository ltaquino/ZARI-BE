namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over Bank Accounts — small master data, no .Include()s needed.
/// BankAccount has no Code or Status field of its own (unlike GlAccount/CostCenter/PaymentMethod/
/// Branch) — its identifying fields are AccountNumber/AccountName/BankName, and BranchId is a
/// required (non-nullable) string. Default sort is AccountName ascending.
/// </summary>
public sealed class BankAccountsReportDataset : IReportDataset
{
    public string Key => "BANK_ACCOUNTS";
    public string Label => "Bank Accounts";
    public string RequiredPermissionCode => "BANK_ACCOUNTS";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("AccountNumber", "Account No.", ReportFieldType.Text),
        new("AccountName", "Account Name", ReportFieldType.Text),
        new("BankName", "Bank Name", ReportFieldType.Text),
        new("BranchId", "Branch", ReportFieldType.Text),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<BankAccount> query = dbContext.BankAccounts.AsNoTracking();

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "AccountNumber" => ReportDatasetFilters.Text(query, filter, b => b.AccountNumber),
                "AccountName" => ReportDatasetFilters.Text(query, filter, b => b.AccountName),
                "BankName" => ReportDatasetFilters.Text(query, filter, b => b.BankName),
                "BranchId" => ReportDatasetFilters.Text(query, filter, b => b.BranchId),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "AccountNumber" => ReportDatasetFilters.Sort(query, request.SortDescending, b => b.AccountNumber),
            "AccountName" => ReportDatasetFilters.Sort(query, request.SortDescending, b => b.AccountName),
            "BankName" => ReportDatasetFilters.Sort(query, request.SortDescending, b => b.BankName),
            "BranchId" => ReportDatasetFilters.Sort(query, request.SortDescending, b => b.BranchId),
            _ => query.OrderBy(b => b.AccountName),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var bankAccounts = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var rows = bankAccounts.Select(b => BuildRow(b, request.ColumnKeys)).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(BankAccount bankAccount, IReadOnlyList<string> columnKeys)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "AccountNumber" => bankAccount.AccountNumber,
                "AccountName" => bankAccount.AccountName,
                "BankName" => bankAccount.BankName,
                "BranchId" => bankAccount.BranchId,
                _ => null,
            };
        }
        return row;
    }
}
