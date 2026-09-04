namespace ZARI.Application.Features.Reporting.ReportTemplates.Shared;

using ZARI.Application.Features.Reporting.Datasets;

/// <summary>
/// The shapes serialized into ReportTemplate.ColumnsJson / FiltersJson / SortJson
/// (System.Text.Json, camelCase). Kept separate from the IReportDataset contract because these
/// describe what a *saved template* chose, not what a dataset makes available.
/// </summary>
/// <summary>
/// Aggregate is null for a plain detail-mode column, or for a column that is itself one of the
/// template's GroupByFieldKeys (its raw, shared-per-group value is shown, not an aggregate). It
/// must be non-null for every OTHER selected column whenever the template's GroupByFieldKeys is
/// non-empty — enforced by Create/Update validation, not by this shape itself.
/// </summary>
public sealed record ReportTemplateColumn(string FieldKey, string Label, int Order, ReportAggregateFunction? Aggregate = null);

/// <summary>
/// PromptAtRuntime=true means Value/Value2 are ignored when saved and the caller must supply a
/// runtime override (see RunReportTemplateQuery.Overrides) every time the template is run — this
/// is what lets a template stay reusable for something like a rolling date range instead of being
/// pinned to whatever dates were used the day it was designed.
/// </summary>
public sealed record ReportTemplateFilter(string FieldKey, ReportFilterOperator Operator, string? Value, string? Value2, bool PromptAtRuntime);

public sealed record ReportTemplateSort(string FieldKey, bool Descending);
