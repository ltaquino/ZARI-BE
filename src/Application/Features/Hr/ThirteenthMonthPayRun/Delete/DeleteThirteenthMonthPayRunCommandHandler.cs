namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>Deleting a DRAFT run cascades its Lines (ThirteenthMonthPayRunConfiguration).</summary>
public sealed class DeleteThirteenthMonthPayRunCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<DeleteThirteenthMonthPayRunCommand>
{
    public async Task<Result> HandleAsync(DeleteThirteenthMonthPayRunCommand command, CancellationToken cancellationToken = default)
    {
        var run = await dbContext.ThirteenthMonthPayRuns.FindAsync([command.Id], cancellationToken);
        if (run is null)
            return Result.Failure(Error.NotFound("ThirteenthMonthPayRun.NotFound", $"13th month pay run with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("13TH_MONTH_PAY_RUNS", FormAction.Delete, run.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("ThirteenthMonthPayRun.Forbidden", "You do not have permission to delete 13th month pay runs for this branch."));

        if (run.Status != "DRAFT")
            return Result.Failure(Error.Validation("ThirteenthMonthPayRun.NotDraft", "Only a draft 13th month pay run can be deleted."));

        dbContext.ThirteenthMonthPayRuns.Remove(run);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
