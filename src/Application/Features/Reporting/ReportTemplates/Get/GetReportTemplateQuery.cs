namespace ZARI.Application.Features.Reporting.ReportTemplates.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Reporting.ReportTemplates.Shared;
using ZARI.Domain.Common;

public sealed record GetReportTemplateQuery(Guid Id) : IQuery<Result<ReportTemplateDetailResponse>>;

/// <summary>Full template definition, columns/filters/sort deserialized — used by the designer's
/// Edit mode and returned by Create.</summary>
public sealed record ReportTemplateDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    string DatasetKey,
    string DatasetLabel,
    string PaperSize,
    string Orientation,
    string? HeaderText,
    string? FooterText,
    bool ShowColumnTotals,
    List<ReportTemplateColumn> Columns,
    List<ReportTemplateFilter> Filters,
    ReportTemplateSort? Sort,
    List<string> GroupByFieldKeys,
    bool IsShared,
    string OwnerUserId,
    bool IsOwner,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt);
