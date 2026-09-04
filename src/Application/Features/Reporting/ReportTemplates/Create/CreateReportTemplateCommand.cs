namespace ZARI.Application.Features.Reporting.ReportTemplates.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Reporting.ReportTemplates.Get;
using ZARI.Application.Features.Reporting.ReportTemplates.Shared;
using ZARI.Domain.Common;

public sealed record CreateReportTemplateCommand(
    string Name,
    string? Description,
    string DatasetKey,
    string PaperSize,
    string Orientation,
    string? HeaderText,
    string? FooterText,
    bool ShowColumnTotals,
    bool IsShared,
    List<ReportTemplateColumn> Columns,
    List<ReportTemplateFilter> Filters,
    ReportTemplateSort? Sort,
    List<string> GroupByFieldKeys) : ICommand<Result<ReportTemplateDetailResponse>>;
