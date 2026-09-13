namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Update;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>
/// Only DRAFT runs can be edited, and only the PayDate — Year is immutable once created since it
/// drives both Compute's lookup and the (BranchId, Year) uniqueness guard.
/// </summary>
public sealed class UpdateThirteenthMonthPayRunCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<UpdateThirteenthMonthPayRunCommand>
{
    public async Task<Result> HandleAsync(UpdateThirteenthMonthPayRunCommand command, CancellationToken cancellationToken = default)
    {
        var run = await dbContext.ThirteenthMonthPayRuns.FindAsync([command.Id], cancellationToken);
        if (run is null)
            return Result.Failure(Error.NotFound("ThirteenthMonthPayRun.NotFound", $"13th month pay run with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("13TH_MONTH_PAY_RUNS", FormAction.Edit, run.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("ThirteenthMonthPayRun.Forbidden", "You do not have permission to update 13th month pay runs for this branch."));

        if (run.Status != "DRAFT")
            return Result.Failure(Error.Validation("ThirteenthMonthPayRun.NotDraft", "Only a draft 13th month pay run can be edited."));

        run.PayDate = command.PayDate;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
