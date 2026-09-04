namespace ZARI.Application.Features.Reporting.ReportTemplates.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllReportTemplatesQuery : IQuery<Result<List<ReportTemplateSummaryResponse>>>;

/// <summary>Lightweight listing row — no Columns/Filters/Sort (that's what Get is for).</summary>
public sealed record ReportTemplateSummaryResponse(
    Guid Id,
    string Name,
    string? Description,
    string DatasetKey,
    string DatasetLabel,
    bool IsShared,
    string OwnerUserId,
    bool IsOwner,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt);
