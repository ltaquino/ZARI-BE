namespace ZARI.Application.Features.Reporting.ReportTemplates.Run;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Reporting.Datasets;
using ZARI.Application.Features.Reporting.ReportTemplates.Shared;
using ZARI.Domain.Common;

public sealed class RunReportTemplateQueryHandler(
    IAppDbContext dbContext,
    IPermissionService permissionService,
    ICurrentUser currentUser,
    IEnumerable<IReportDataset> datasets) : IQueryHandler<RunReportTemplateQuery, Result<RunReportTemplateResponse>>
{
    private const int RowCap = 20000;

    // Grouped mode's response shrinks to however many groups exist, regardless of how many raw
    // rows were scanned to build them — so it's worth scanning further before rows are collapsed
    // by ReportAggregationEngine, to get a more complete/accurate set of groups and totals. This is
    // a disclosed limitation, not silent: if truncation still happens at 200k raw rows, the
    // response's existing Truncated flag still communicates it — no new field needed.
    private const int GroupedRowCap = 200_000;

    public async Task<Result<RunReportTemplateResponse>> HandleAsync(RunReportTemplateQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("REPORT_DESIGNER", FormAction.View, cancellationToken))
            return Result.Failure<RunReportTemplateResponse>(Error.Forbidden("ReportTemplate.Forbidden", "You do not have permission to view report templates."));

        var template = await dbContext.ReportTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == query.TemplateId, cancellationToken);
        if (template is null)
            return Result.Failure<RunReportTemplateResponse>(Error.NotFound("ReportTemplate.NotFound", $"Report template with ID '{query.TemplateId}' was not found."));

        var isOwner = template.OwnerUserId == currentUser.UserId;
        if (!template.IsShared && !isOwner)
            return Result.Failure<RunReportTemplateResponse>(Error.Forbidden("ReportTemplate.Forbidden", "You do not have access to this report template."));

        var dataset = datasets.FirstOrDefault(d => d.Key == template.DatasetKey);
        if (dataset is null)
            return Result.Failure<RunReportTemplateResponse>(Error.Validation("ReportTemplate.DatasetMissing", $"Dataset '{template.DatasetKey}' is no longer available."));

        // Defense in depth: matters most for a *shared* template built by someone with broader
        // access than the person now running it.
        if (!await permissionService.HasPermissionAsync(dataset.RequiredPermissionCode, FormAction.View, cancellationToken))
            return Result.Failure<RunReportTemplateResponse>(Error.Forbidden("ReportTemplate.Forbidden", "You do not have permission to view the underlying data for this dataset."));

        var columns = JsonSerializer.Deserialize<List<ReportTemplateColumn>>(template.ColumnsJson) ?? [];
        var filters = JsonSerializer.Deserialize<List<ReportTemplateFilter>>(template.FiltersJson) ?? [];
        var sort = template.SortJson is null ? null : JsonSerializer.Deserialize<ReportTemplateSort>(template.SortJson);
        var groupByFieldKeys = JsonSerializer.Deserialize<List<string>>(template.GroupByJson) ?? [];
        var isGrouped = groupByFieldKeys.Count > 0;

        var overridesByKey = query.Overrides.ToDictionary(o => o.FieldKey, o => o);

        // For a prompted filter with no override supplied, pass through null and let the dataset
        // simply not filter on it — permissive by design, not a hard failure.
        var effectiveFilters = filters
            .Select(f =>
            {
                if (!f.PromptAtRuntime)
                    return new ReportFilterValue(f.FieldKey, f.Operator, f.Value, f.Value2);

                return overridesByKey.TryGetValue(f.FieldKey, out var ov)
                    ? new ReportFilterValue(f.FieldKey, f.Operator, ov.Value, ov.Value2)
                    : new ReportFilterValue(f.FieldKey, f.Operator, null, null);
            })
            .ToList();

        var orderedColumns = columns.OrderBy(c => c.Order).ToList();
        var fieldsByKey = dataset.Fields.ToDictionary(f => f.Key);

        // In grouped mode the dataset must also return every group-by field, even one that isn't
        // itself a selected column key elsewhere — ReportAggregationEngine needs every group-by
        // field's raw value on every row to build the composite grouping key. (In practice
        // Create/Update validation already requires every group-by field to be a selected column,
        // so this union is a defensive no-op today, not a behavior change.)
        var columnKeys = isGrouped
            ? orderedColumns.Select(c => c.FieldKey).Union(groupByFieldKeys).ToList()
            : orderedColumns.Select(c => c.FieldKey).ToList();

        // Always-on branch access enforcement — never something the template itself configures or
        // that a user can see/remove, same as any other server-side authorization check.
        var branchScopeFilters = await ReportBranchScope.BuildAsync(dbContext, currentUser, dataset, cancellationToken);

        var request = new ReportDatasetRunRequest(
            ColumnKeys: columnKeys,
            Filters: [.. effectiveFilters, .. branchScopeFilters],
            SortFieldKey: sort?.FieldKey,
            SortDescending: sort?.Descending ?? false,
            RowCap: isGrouped ? GroupedRowCap : RowCap);

        var runResult = await dataset.RunAsync(dbContext, request, cancellationToken);

        var groupBySet = groupByFieldKeys.ToHashSet();
        var responseColumns = orderedColumns
            .Select(c => new RunReportTemplateColumnResponse(
                c.FieldKey,
                isGrouped && c.Aggregate is not null && !groupBySet.Contains(c.FieldKey)
                    ? $"{c.Label} ({c.Aggregate})"
                    : c.Label,
                fieldsByKey.TryGetValue(c.FieldKey, out var fieldDefinition) ? fieldDefinition.Type.ToString() : "Text"))
            .ToList();

        IReadOnlyList<IReadOnlyDictionary<string, object?>> effectiveRows;
        int totalRows;

        if (isGrouped)
        {
            var (groupedRows, groupCount) = ReportAggregationEngine.Apply(runResult.Rows, groupByFieldKeys, orderedColumns, sort);
            effectiveRows = groupedRows;
            totalRows = groupCount;
        }
        else
        {
            effectiveRows = runResult.Rows;
            totalRows = runResult.TotalRows;
        }

        var rows = effectiveRows.Select(r => new Dictionary<string, object?>(r)).ToList();

        var response = new RunReportTemplateResponse(
            template.Name,
            template.HeaderText,
            template.FooterText,
            template.PaperSize,
            template.Orientation,
            template.ShowColumnTotals,
            responseColumns,
            rows,
            runResult.Truncated,
            totalRows);

        return Result.Success(response);
    }
}
