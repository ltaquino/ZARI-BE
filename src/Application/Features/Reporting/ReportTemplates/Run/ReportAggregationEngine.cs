namespace ZARI.Application.Features.Reporting.ReportTemplates.Run;

using ZARI.Application.Features.Reporting.Datasets;
using ZARI.Application.Features.Reporting.ReportTemplates.Shared;

/// <summary>
/// Collapses flat dataset rows into one row per distinct combination of GroupByFieldKeys values,
/// computing each non-group-by column's Aggregate function over the members of its group. Only
/// invoked by RunReportTemplateQueryHandler when the template's GroupByFieldKeys is non-empty --
/// plain detail-mode templates never reach this class.
/// </summary>
public static class ReportAggregationEngine
{
    // Joins each group-by field's raw value for the composite grouping key. \u0001 is a control
    // character no real field value is realistically going to contain, so it won't collide.
    private const string KeySeparator = "\u0001";

    public static (List<IReadOnlyDictionary<string, object?>> Rows, int GroupCount) Apply(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        IReadOnlyList<string> groupByFieldKeys,
        IReadOnlyList<ReportTemplateColumn> orderedColumns,
        ReportTemplateSort? sort)
    {
        var bucketsByKey = new Dictionary<string, int>();
        var sharedValuesByBucket = new List<Dictionary<string, object?>>();
        var membersByBucket = new List<List<IReadOnlyDictionary<string, object?>>>();

        foreach (var row in rows)
        {
            var compositeKey = string.Join(
                KeySeparator,
                groupByFieldKeys.Select(k => row.TryGetValue(k, out var v) ? (v?.ToString() ?? string.Empty) : string.Empty));

            if (!bucketsByKey.TryGetValue(compositeKey, out var bucketIndex))
            {
                var sharedValues = new Dictionary<string, object?>();
                foreach (var key in groupByFieldKeys)
                    sharedValues[key] = row.TryGetValue(key, out var v) ? v : null;

                bucketIndex = sharedValuesByBucket.Count;
                bucketsByKey[compositeKey] = bucketIndex;
                sharedValuesByBucket.Add(sharedValues);
                membersByBucket.Add([]);
            }

            membersByBucket[bucketIndex].Add(row);
        }

        var groupBySet = groupByFieldKeys.ToHashSet();

        var outputRows = new List<IReadOnlyDictionary<string, object?>>();
        for (var i = 0; i < sharedValuesByBucket.Count; i++)
        {
            var sharedValues = sharedValuesByBucket[i];
            var members = membersByBucket[i];
            var outputRow = new Dictionary<string, object?>();

            foreach (var column in orderedColumns)
            {
                if (groupBySet.Contains(column.FieldKey))
                {
                    outputRow[column.FieldKey] = sharedValues.TryGetValue(column.FieldKey, out var shared) ? shared : null;
                }
                else
                {
                    var rawValues = members.Select(m => m.TryGetValue(column.FieldKey, out var v) ? v : null).ToList();
                    outputRow[column.FieldKey] = ComputeAggregate(column.Aggregate, rawValues);
                }
            }

            outputRows.Add(outputRow);
        }

        if (sort is not null && orderedColumns.Any(c => c.FieldKey == sort.FieldKey))
        {
            outputRows = sort.Descending
                ? outputRows.OrderByDescending(r => r.TryGetValue(sort.FieldKey, out var v) ? v : null, ValueComparer.Instance).ToList()
                : outputRows.OrderBy(r => r.TryGetValue(sort.FieldKey, out var v) ? v : null, ValueComparer.Instance).ToList();
        }

        return (outputRows, sharedValuesByBucket.Count);
    }

    private static object? ComputeAggregate(ReportAggregateFunction? aggregate, List<object?> rawValues)
    {
        if (aggregate is null) return null;

        // Count is meaningful for text fields too (counts non-null/non-empty values) -- everything
        // else goes through the same decimal coercion used everywhere else in report rendering.
        if (aggregate == ReportAggregateFunction.Count)
            return rawValues.Count(v => v is not null && (v is not string s || !string.IsNullOrEmpty(s)));

        var decimals = rawValues.Select(ToDecimalOrZero).ToList();
        return aggregate switch
        {
            ReportAggregateFunction.Sum => decimals.Sum(),
            ReportAggregateFunction.Avg => decimals.Count == 0 ? 0m : decimals.Average(),
            ReportAggregateFunction.Min => decimals.Count == 0 ? 0m : decimals.Min(),
            ReportAggregateFunction.Max => decimals.Count == 0 ? 0m : decimals.Max(),
            _ => null
        };
    }

    // Mirrors GenericTableReportDocument's private ToDecimalOrZero -- Application can't reference
    // the Api project, so the same 3-line pattern is re-implemented locally rather than shared.
    private static decimal ToDecimalOrZero(object? value)
    {
        if (value is null) return 0m;
        try { return Convert.ToDecimal(value); } catch { return 0m; }
    }

    /// <summary>Numeric-aware when both sides coerce to decimal, ordinal string comparison
    /// otherwise (e.g. text fields, dates left as strings, or a null on one side).</summary>
    private sealed class ValueComparer : IComparer<object?>
    {
        public static readonly ValueComparer Instance = new();

        public int Compare(object? x, object? y)
        {
            if (TryToDecimal(x, out var dx) && TryToDecimal(y, out var dy))
                return dx.CompareTo(dy);

            return string.Compare(x?.ToString() ?? string.Empty, y?.ToString() ?? string.Empty, StringComparison.Ordinal);
        }

        private static bool TryToDecimal(object? value, out decimal result)
        {
            result = 0m;
            if (value is null) return false;
            try { result = Convert.ToDecimal(value); return true; } catch { return false; }
        }
    }
}
