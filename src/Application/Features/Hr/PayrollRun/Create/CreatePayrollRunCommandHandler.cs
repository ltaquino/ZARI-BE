namespace ZARI.Application.Features.Hr.PayrollRuns.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.PayrollRuns.Get;
using ZARI.Application.Features.Hr.PayrollRuns.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreatePayrollRunCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreatePayrollRunCommand, Result<PayrollRunResponse>>
{
    public async Task<Result<PayrollRunResponse>> HandleAsync(CreatePayrollRunCommand command, CancellationToken cancellationToken = default)
    {
        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<PayrollRunResponse>(Error.NotFound("Branch.NotFound", $"Branch '{command.BranchId}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("PAYROLL_RUNS", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<PayrollRunResponse>(Error.Forbidden("PayrollRun.Forbidden", "You do not have permission to create payroll runs for this branch."));

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "PAYROLL-RUN"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<PayrollRunResponse>(numberResult.Error!);

        var run = new PayrollRun
        {
            PayrollRunNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            PayPeriodStart = command.PayPeriodStart,
            PayPeriodEnd = command.PayPeriodEnd,
            PayDate = command.PayDate,
            Status = "DRAFT"
        };

        dbContext.PayrollRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(PayrollRunMapper.ToResponse(run));
    }
}
