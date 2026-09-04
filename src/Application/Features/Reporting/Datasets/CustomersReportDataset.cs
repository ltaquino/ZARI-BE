namespace ZARI.Application.Features.Reporting.Datasets;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Report Designer dataset over Customer master data — every field here is a plain scalar column,
/// so unlike this module's transactional datasets, nothing here needs an in-memory computed column:
/// every field stays fully filterable/sortable at the SQL level. Customer has no Code, ContactPerson
/// or CreditLimit property (unlike, say, Supplier's own fields elsewhere in this codebase), so those
/// proposed fields are replaced here with what the entity actually exposes: Type, Owner (the
/// salesperson/account-owner display name), Address, MemberNo (free-text cooperative member number)
/// and PaymentTermsDays. Master data has no natural transaction date to default-sort by, so this
/// defaults to Name ascending rather than a date field.
/// </summary>
public sealed class CustomersReportDataset : IReportDataset
{
    public string Key => "CUSTOMERS";
    public string Label => "Customers";
    public string RequiredPermissionCode => "CUSTOMERS";

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } =
    [
        new("Name", "Name", ReportFieldType.Text),
        new("Type", "Type", ReportFieldType.Text),
        new("Email", "Email", ReportFieldType.Text),
        new("Phone", "Phone", ReportFieldType.Text),
        new("Address", "Address", ReportFieldType.Text),
        new("Owner", "Owner", ReportFieldType.Text),
        new("BranchId", "Branch", ReportFieldType.Text),
        new("Status", "Status", ReportFieldType.Text),
        new("MemberNo", "Member No.", ReportFieldType.Text),
        new("PaymentTermsDays", "Payment Terms (Days)", ReportFieldType.Number),
    ];

    public async Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        IQueryable<Customer> query = dbContext.Customers.AsNoTracking();

        foreach (var filter in request.Filters)
        {
            query = filter.FieldKey switch
            {
                "Name" => ReportDatasetFilters.Text(query, filter, c => c.Name),
                "Type" => ReportDatasetFilters.Text(query, filter, c => c.Type),
                "Email" => ReportDatasetFilters.Text(query, filter, c => c.Email),
                "Phone" => ReportDatasetFilters.Text(query, filter, c => c.Phone),
                "Address" => ReportDatasetFilters.Text(query, filter, c => c.Address),
                "Owner" => ReportDatasetFilters.Text(query, filter, c => c.Owner),
                "BranchId" => ReportDatasetFilters.Text(query, filter, c => c.BranchId),
                "Status" => ReportDatasetFilters.Text(query, filter, c => c.Status),
                "MemberNo" => ReportDatasetFilters.Text(query, filter, c => c.MemberNo),
                "PaymentTermsDays" => ReportDatasetFilters.Decimal(query, filter, c => (decimal?)c.PaymentTermsDays),
                _ => query,
            };
        }

        query = request.SortFieldKey switch
        {
            "Name" => ReportDatasetFilters.Sort(query, request.SortDescending, c => c.Name),
            "Type" => ReportDatasetFilters.Sort(query, request.SortDescending, c => c.Type),
            "Email" => ReportDatasetFilters.Sort(query, request.SortDescending, c => c.Email),
            "Phone" => ReportDatasetFilters.Sort(query, request.SortDescending, c => c.Phone),
            "Address" => ReportDatasetFilters.Sort(query, request.SortDescending, c => c.Address),
            "Owner" => ReportDatasetFilters.Sort(query, request.SortDescending, c => c.Owner),
            "BranchId" => ReportDatasetFilters.Sort(query, request.SortDescending, c => c.BranchId),
            "Status" => ReportDatasetFilters.Sort(query, request.SortDescending, c => c.Status),
            "MemberNo" => ReportDatasetFilters.Sort(query, request.SortDescending, c => c.MemberNo),
            "PaymentTermsDays" => ReportDatasetFilters.Sort(query, request.SortDescending, c => c.PaymentTermsDays),
            _ => query.OrderBy(c => c.Name),
        };

        var capped = await query.Take(request.RowCap + 1).ToListAsync(cancellationToken);
        var truncated = capped.Count > request.RowCap;
        var customers = truncated ? capped.Take(request.RowCap).ToList() : capped;

        var rows = customers.Select(c => BuildRow(c, request.ColumnKeys)).ToList();
        return new ReportDatasetRunResult(rows, truncated, rows.Count);
    }

    private static IReadOnlyDictionary<string, object?> BuildRow(Customer customer, IReadOnlyList<string> columnKeys)
    {
        var row = new Dictionary<string, object?>();
        foreach (var key in columnKeys)
        {
            row[key] = key switch
            {
                "Name" => customer.Name,
                "Type" => customer.Type,
                "Email" => customer.Email,
                "Phone" => customer.Phone,
                "Address" => customer.Address,
                "Owner" => customer.Owner,
                "BranchId" => customer.BranchId,
                "Status" => customer.Status,
                "MemberNo" => customer.MemberNo,
                "PaymentTermsDays" => customer.PaymentTermsDays,
                _ => null,
            };
        }
        return row;
    }
}
