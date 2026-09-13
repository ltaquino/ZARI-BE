namespace ZARI.Application.Features.Hr.PayrollRuns.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>Deleting a DRAFT run cascades its Lines (PayrollRunConfiguration).</summary>
public sealed class DeletePayrollRunCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<DeletePayrollRunCommand>
{
    public async Task<Result> HandleAsync(DeletePayrollRunCommand command, CancellationToken cancellationToken = default)
    {
        var run = await dbContext.PayrollRuns.FindAsync([command.Id], cancellationToken);
        if (run is null)
            return Result.Failure(Error.NotFound("PayrollRun.NotFound", $"Payroll run with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("PAYROLL_RUNS", FormAction.Delete, run.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("PayrollRun.Forbidden", "You do not have permission to delete payroll runs for this branch."));

        if (run.Status != "DRAFT")
            return Result.Failure(Error.Validation("PayrollRun.NotDraft", "Only a draft payroll run can be deleted."));

        dbContext.PayrollRuns.Remove(run);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
