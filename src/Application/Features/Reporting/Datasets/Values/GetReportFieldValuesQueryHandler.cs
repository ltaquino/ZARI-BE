namespace ZARI.Application.Features.Reporting.Datasets.Values;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Reporting.Datasets;
using ZARI.Domain.Common;

/// <summary>
/// Powers a searchable-dropdown filter-value picker in the Report Designer/Viewer, generically for
/// EVERY dataset/field with zero per-dataset code — reuses the exact same <see cref="IReportDataset.RunAsync"/>
/// contract every dataset already implements for its own filtering, rather than adding a bespoke
/// "list distinct values" method to the interface. A Contains filter on the target field (when
/// <see cref="GetReportFieldValuesQuery.Search"/> is supplied) is passed straight into RunAsync, so
/// the underlying SQL predicate is still whatever that dataset's own filter switch already builds —
/// no new query logic needed per entity, and this works for all 26 datasets (and any future ones)
/// without ever touching their files.
/// </summary>
public sealed class GetReportFieldValuesQueryHandler(
    IAppDbContext dbContext,
    IPermissionService permissionService,
    IEnumerable<IReportDataset> datasets) : IQueryHandler<GetReportFieldValuesQuery, Result<List<string>>>
{
    // Cheap enough for "give the user a reasonable set of real values to pick from," not meant to
    // be an exhaustive scan of a huge table — same bounded/occasional-use precedent used everywhere
    // else in this module (GetInventoryAsOfQueryHandler, ReportAggregationEngine's grouped cap). A
    // field with more distinct values than MaxDistinctValues still works fine as free text — the
    // input this feeds always lets the user type a value that isn't in the returned list.
    private const int ScanRowCap = 5000;
    private const int MaxDistinctValues = 300;

    public async Task<Result<List<string>>> HandleAsync(GetReportFieldValuesQuery query, CancellationToken cancellationToken = default)
    {
        var dataset = datasets.FirstOrDefault(d => d.Key == query.DatasetKey);
        if (dataset is null)
            return Result.Failure<List<string>>(Error.Validation("ReportTemplate.InvalidDataset", $"'{query.DatasetKey}' is not a recognized report dataset."));

        if (!await permissionService.HasPermissionAsync(dataset.RequiredPermissionCode, FormAction.View, cancellationToken))
            return Result.Failure<List<string>>(Error.Forbidden("ReportTemplate.Forbidden", "You do not have permission to view the underlying data for this dataset."));

        var field = dataset.Fields.FirstOrDefault(f => f.Key == query.FieldKey);
        if (field is null)
            return Result.Failure<List<string>>(Error.Validation("ReportTemplate.UnknownField", $"'{query.FieldKey}' is not a field on dataset '{dataset.Key}'."));

        if (!field.Filterable)
            return Result.Failure<List<string>>(Error.Validation("ReportTemplate.FieldNotFilterable", $"'{query.FieldKey}' does not support filtering."));

        List<ReportFilterValue> filters = string.IsNullOrWhiteSpace(query.Search)
            ? []
            : [new ReportFilterValue(query.FieldKey, ReportFilterOperator.Contains, query.Search)];

        var request = new ReportDatasetRunRequest(
            ColumnKeys: [query.FieldKey],
            Filters: filters,
            SortFieldKey: field.Sortable ? query.FieldKey : null,
            SortDescending: false,
            RowCap: ScanRowCap);

        var runResult = await dataset.RunAsync(dbContext, request, cancellationToken);

        var values = runResult.Rows
            .Select(r => r.TryGetValue(query.FieldKey, out var v) ? v?.ToString() : null)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .Take(MaxDistinctValues)
            .ToList();

        return Result.Success(values);
    }
}
