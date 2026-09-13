namespace ZARI.Application.Features.Hr.PayrollRuns.Update;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>
/// Only DRAFT runs can be edited. Editing the pay period here does not touch already-generated
/// Lines — re-run Compute afterward to regenerate them against the new period.
/// </summary>
public sealed class UpdatePayrollRunCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<UpdatePayrollRunCommand>
{
    public async Task<Result> HandleAsync(UpdatePayrollRunCommand command, CancellationToken cancellationToken = default)
    {
        var run = await dbContext.PayrollRuns.FindAsync([command.Id], cancellationToken);
        if (run is null)
            return Result.Failure(Error.NotFound("PayrollRun.NotFound", $"Payroll run with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("PAYROLL_RUNS", FormAction.Edit, run.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("PayrollRun.Forbidden", "You do not have permission to update payroll runs for this branch."));

        if (run.Status != "DRAFT")
            return Result.Failure(Error.Validation("PayrollRun.NotDraft", "Only a draft payroll run can be edited."));

        run.PayPeriodStart = command.PayPeriodStart;
        run.PayPeriodEnd = command.PayPeriodEnd;
        run.PayDate = command.PayDate;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
