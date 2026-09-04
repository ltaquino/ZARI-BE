namespace ZARI.Application.Features.Reporting.ReportTemplates.Run;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>A runtime value supplied for one of the template's PromptAtRuntime=true filters,
/// matched to that filter by FieldKey.</summary>
public sealed record RunReportTemplateFilterOverride(string FieldKey, string? Value, string? Value2);

public sealed record RunReportTemplateQuery(Guid TemplateId, List<RunReportTemplateFilterOverride> Overrides) : IQuery<Result<RunReportTemplateResponse>>;

public sealed record RunReportTemplateColumnResponse(string FieldKey, string Label, string Type);

public sealed record RunReportTemplateResponse(
    string TemplateName,
    string? HeaderText,
    string? FooterText,
    string PaperSize,
    string Orientation,
    bool ShowColumnTotals,
    List<RunReportTemplateColumnResponse> Columns,
    List<Dictionary<string, object?>> Rows,
    bool Truncated,
    int TotalRows);
