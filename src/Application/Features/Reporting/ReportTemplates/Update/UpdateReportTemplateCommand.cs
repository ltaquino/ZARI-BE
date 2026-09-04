namespace ZARI.Application.Features.Reporting.ReportTemplates.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Reporting.ReportTemplates.Shared;

public sealed record UpdateReportTemplateCommand(
    Guid Id,
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
    List<string> GroupByFieldKeys) : ICommand;
