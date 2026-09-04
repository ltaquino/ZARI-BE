namespace ZARI.Application.Features.Reporting.ReportTemplates.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteReportTemplateCommand(Guid Id) : ICommand;
