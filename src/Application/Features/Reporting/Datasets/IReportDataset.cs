namespace ZARI.Application.Features.Reporting.Datasets;

using System.Text.Json.Serialization;
using ZARI.Application.Abstractions.Data;

/// <summary>
/// One entry in the fixed, hand-coded "reportable dataset" catalog that backs the Report
/// Designer module. Each implementation declares which fields it exposes and knows how to filter/
/// project its own source data — there is no dynamic-LINQ or string-based query parsing anywhere;
/// every field key a caller can ever pass through is one this dataset itself declared in
/// <see cref="Fields"/>, so a template can never reach a column or predicate its author didn't
/// explicitly wire up. Implementations are stateless and registered as singletons (see
/// Application/DependencyInjection.cs) — all per-request state is passed into <see cref="RunAsync"/>.
/// </summary>
public interface IReportDataset
{
    /// <summary>Stable identifier stored on a ReportTemplate, e.g. "SALES_INVOICES".</summary>
    string Key { get; }

    /// <summary>Display label for the dataset picker, e.g. "Sales Invoices".</summary>
    string Label { get; }

    /// <summary>
    /// The existing permission code that already gates this data elsewhere in the app (e.g.
    /// "SALES_INVOICES") — a user can only build/run a report over data they could already see
    /// through that entity's own pages. No new per-dataset permission is introduced.
    /// </summary>
    string RequiredPermissionCode { get; }

    /// <summary>The fields this dataset exposes to the designer's field/filter pickers.</summary>
    IReadOnlyList<ReportFieldDefinition> Fields { get; }

    Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken);
}

public enum ReportFieldType
{
    Text,
    Number,
    Currency,
    Date,
    Boolean
}

// Every other "enumerated value" in this codebase (Status, VatType, PaperSize, ...) is modeled as
// a plain string, so nothing elsewhere has ever needed enum JSON conversion. ReportFilterOperator
// is the first C# enum that actually crosses the wire (in ReportTemplateFilter, part of the
// Create/Update/Get request+response bodies) — scope the string conversion to just this enum
// type via the attribute below rather than a global JsonOptions change, so this stays a
// zero-blast-radius fix with no effect on any of the app's other 65+ existing endpoints.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportFilterOperator
{
    Equals,
    NotEquals,
    Contains,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Between,
    In
}

// Same rationale/treatment as ReportFilterOperator above: readable strings over the wire from
// day one, scoped to just this one new enum type.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportAggregateFunction
{
    Sum,
    Avg,
    Count,
    Min,
    Max
}

public sealed record ReportFieldDefinition(string Key, string Label, ReportFieldType Type, bool Filterable = true, bool Sortable = true);

/// <summary>
/// One filter condition to apply. <see cref="Value"/> holds the single comparison value (for
/// Equals/NotEquals/Contains/GreaterThan(OrEqual)/LessThan(OrEqual)), the lower bound (for
/// Between), or a comma-separated value list (for In). <see cref="Value2"/> holds the upper bound
/// for Between only. Date values are ISO-8601 strings ("2026-09-04"); Boolean values are
/// "true"/"false"; Number/Currency values are invariant-culture decimal strings.
/// </summary>
public sealed record ReportFilterValue(string FieldKey, ReportFilterOperator Operator, string? Value, string? Value2 = null);

public sealed record ReportDatasetRunRequest(
    IReadOnlyList<string> ColumnKeys,
    IReadOnlyList<ReportFilterValue> Filters,
    string? SortFieldKey,
    bool SortDescending,
    int RowCap);

/// <summary>
/// Row values are keyed by field Key, matching whatever subset of <see cref="ReportDatasetRunRequest.ColumnKeys"/>
/// was requested. Value CLR types follow each field's declared ReportFieldType (string, decimal,
/// DateTimeOffset/DateOnly, bool) so the API layer can serialize them directly and the PDF/FE
/// layers can format them per-type without re-parsing.
/// </summary>
public sealed record ReportDatasetRunResult(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows,
    bool Truncated,
    int TotalRows);
