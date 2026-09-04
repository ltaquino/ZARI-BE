namespace ZARI.Application.Features.Reporting.ReportTemplates.Get;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Reporting.Datasets;
using ZARI.Application.Features.Reporting.ReportTemplates.Shared;
using ZARI.Domain.Common;

public sealed class GetReportTemplateQueryHandler(
    IAppDbContext dbContext,
    IPermissionService permissionService,
    ICurrentUser currentUser,
    IEnumerable<IReportDataset> datasets) : IQueryHandler<GetReportTemplateQuery, Result<ReportTemplateDetailResponse>>
{
    public async Task<Result<ReportTemplateDetailResponse>> HandleAsync(GetReportTemplateQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("REPORT_DESIGNER", FormAction.View, cancellationToken))
            return Result.Failure<ReportTemplateDetailResponse>(Error.Forbidden("ReportTemplate.Forbidden", "You do not have permission to view report templates."));

        var template = await dbContext.ReportTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == query.Id, cancellationToken);
        if (template is null)
            return Result.Failure<ReportTemplateDetailResponse>(Error.NotFound("ReportTemplate.NotFound", $"Report template with ID '{query.Id}' was not found."));

        var isOwner = template.OwnerUserId == currentUser.UserId;
        if (!template.IsShared && !isOwner)
            return Result.Failure<ReportTemplateDetailResponse>(Error.Forbidden("ReportTemplate.Forbidden", "You do not have access to this report template."));

        var dataset = datasets.FirstOrDefault(d => d.Key == template.DatasetKey);
        var datasetLabel = dataset?.Label ?? template.DatasetKey;

        var columns = JsonSerializer.Deserialize<List<ReportTemplateColumn>>(template.ColumnsJson) ?? [];
        var filters = JsonSerializer.Deserialize<List<ReportTemplateFilter>>(template.FiltersJson) ?? [];
        var sort = template.SortJson is null ? null : JsonSerializer.Deserialize<ReportTemplateSort>(template.SortJson);
        var groupByFieldKeys = JsonSerializer.Deserialize<List<string>>(template.GroupByJson) ?? [];

        var response = new ReportTemplateDetailResponse(
            template.Id,
            template.Name,
            template.Description,
            template.DatasetKey,
            datasetLabel,
            template.PaperSize,
            template.Orientation,
            template.HeaderText,
            template.FooterText,
            template.ShowColumnTotals,
            columns,
            filters,
            sort,
            groupByFieldKeys,
            template.IsShared,
            template.OwnerUserId,
            isOwner,
            template.CreatedAt,
            template.LastModifiedAt);

        return Result.Success(response);
    }
}
