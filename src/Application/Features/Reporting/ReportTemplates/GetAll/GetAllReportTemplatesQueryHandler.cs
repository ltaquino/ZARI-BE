namespace ZARI.Application.Features.Reporting.ReportTemplates.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Reporting.Datasets;
using ZARI.Domain.Common;

public sealed class GetAllReportTemplatesQueryHandler(
    IAppDbContext dbContext,
    IPermissionService permissionService,
    ICurrentUser currentUser,
    IEnumerable<IReportDataset> datasets) : IQueryHandler<GetAllReportTemplatesQuery, Result<List<ReportTemplateSummaryResponse>>>
{
    public async Task<Result<List<ReportTemplateSummaryResponse>>> HandleAsync(GetAllReportTemplatesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("REPORT_DESIGNER", FormAction.View, cancellationToken))
            return Result.Failure<List<ReportTemplateSummaryResponse>>(Error.Forbidden("ReportTemplate.Forbidden", "You do not have permission to view report templates."));

        var userId = currentUser.UserId;

        var templates = await dbContext.ReportTemplates.AsNoTracking()
            .Where(t => t.OwnerUserId == userId || t.IsShared)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        var datasetLabels = datasets.ToDictionary(d => d.Key, d => d.Label);

        var items = templates
            .Select(t => new ReportTemplateSummaryResponse(
                t.Id,
                t.Name,
                t.Description,
                t.DatasetKey,
                datasetLabels.TryGetValue(t.DatasetKey, out var label) ? label : t.DatasetKey,
                t.IsShared,
                t.OwnerUserId,
                t.OwnerUserId == userId,
                t.CreatedAt,
                t.LastModifiedAt))
            .ToList();

        return Result.Success(items);
    }
}
