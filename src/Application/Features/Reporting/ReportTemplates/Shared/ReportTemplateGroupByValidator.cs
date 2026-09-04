namespace ZARI.Application.Features.Reporting.ReportTemplates.Shared;

using ZARI.Domain.Common;

/// <summary>
/// Shared Create/Update validation for a template's GroupByFieldKeys, factored out because both
/// handlers need the exact same three rules. Only called when GroupByFieldKeys is non-empty —
/// an empty list (plain detail-mode report) skips all of this by design.
/// </summary>
internal static class ReportTemplateGroupByValidator
{
    /// <summary>Returns the first validation Error found, or null if GroupByFieldKeys is empty or
    /// every rule passes.</summary>
    public static Error? Validate(
        IReadOnlyList<string> groupByFieldKeys,
        IReadOnlyList<ReportTemplateColumn> columns,
        IReadOnlySet<string> validDatasetFieldKeys,
        string datasetKey)
    {
        if (groupByFieldKeys.Count == 0) return null;

        var columnFieldKeys = columns.Select(c => c.FieldKey).ToHashSet();

        foreach (var key in groupByFieldKeys)
        {
            if (!validDatasetFieldKeys.Contains(key))
                return Error.Validation("ReportTemplate.UnknownField", $"'{key}' is not a field on dataset '{datasetKey}'.");

            if (!columnFieldKeys.Contains(key))
                return Error.Validation("ReportTemplate.GroupByFieldNotSelected", $"'{key}' must also be a selected column to be used as a group-by field.");
        }

        var groupBySet = groupByFieldKeys.ToHashSet();

        foreach (var column in columns)
        {
            if (!groupBySet.Contains(column.FieldKey) && column.Aggregate is null)
                return Error.Validation("ReportTemplate.MissingAggregate", $"'{column.FieldKey}' must have an aggregate function (Sum/Avg/Count/Min/Max) since this report is grouped.");
        }

        return null;
    }

    /// <summary>Clears Aggregate on any column that is itself a group-by field — silently, per
    /// design (a group-by column shows its raw shared-per-group value, never an aggregate), never
    /// rejected as a validation error.</summary>
    public static List<ReportTemplateColumn> NormalizeColumns(
        IReadOnlyList<ReportTemplateColumn> columns,
        IReadOnlyList<string> groupByFieldKeys)
    {
        if (groupByFieldKeys.Count == 0) return columns.ToList();

        var groupBySet = groupByFieldKeys.ToHashSet();
        return columns
            .Select(c => groupBySet.Contains(c.FieldKey) && c.Aggregate is not null ? c with { Aggregate = null } : c)
            .ToList();
    }
}
