namespace ZARI.Application.Features.Reporting.ReportTemplates.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteReportTemplateCommandHandler(
    IAppDbContext dbContext,
    IPermissionService permissionService,
    ICurrentUser currentUser) : ICommandHandler<DeleteReportTemplateCommand>
{
    public async Task<Result> HandleAsync(DeleteReportTemplateCommand command, CancellationToken cancellationToken = default)
    {
        var template = await dbContext.ReportTemplates.FindAsync([command.Id], cancellationToken);
        if (template is null)
            return Result.Failure(Error.NotFound("ReportTemplate.NotFound", $"Report template with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("REPORT_DESIGNER", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("ReportTemplate.Forbidden", "You do not have permission to delete report templates."));

        // Same ownership rule as Update.
        if (template.OwnerUserId != currentUser.UserId)
            return Result.Failure(Error.Forbidden("ReportTemplate.NotOwner", "Only the owner of this report template may delete it."));

        dbContext.ReportTemplates.Remove(template);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
