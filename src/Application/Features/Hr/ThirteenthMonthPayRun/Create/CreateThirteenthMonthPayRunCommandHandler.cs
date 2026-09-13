namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Get;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateThirteenthMonthPayRunCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreateThirteenthMonthPayRunCommand, Result<ThirteenthMonthPayRunResponse>>
{
    public async Task<Result<ThirteenthMonthPayRunResponse>> HandleAsync(CreateThirteenthMonthPayRunCommand command, CancellationToken cancellationToken = default)
    {
        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<ThirteenthMonthPayRunResponse>(Error.NotFound("Branch.NotFound", $"Branch '{command.BranchId}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("13TH_MONTH_PAY_RUNS", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<ThirteenthMonthPayRunResponse>(Error.Forbidden("ThirteenthMonthPayRun.Forbidden", "You do not have permission to create 13th month pay runs for this branch."));

        var duplicateExists = await dbContext.ThirteenthMonthPayRuns.AnyAsync(r => r.BranchId == command.BranchId && r.Year == command.Year, cancellationToken);
        if (duplicateExists)
            return Result.Failure<ThirteenthMonthPayRunResponse>(Error.Conflict("ThirteenthMonthPayRun.Duplicate", $"A 13th month pay run for {command.Year} already exists for this branch."));

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "13TH-PAY"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<ThirteenthMonthPayRunResponse>(numberResult.Error!);

        var run = new ThirteenthMonthPayRun
        {
            RunNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            Year = command.Year,
            PayDate = command.PayDate,
            Status = "DRAFT"
        };

        dbContext.ThirteenthMonthPayRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(ThirteenthMonthPayRunMapper.ToResponse(run));
    }
}
