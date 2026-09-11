namespace ZARI.Application.Features.Loan.LoanWriteOffs.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanRestructurings.Shared;
using ZARI.Application.Features.Loan.LoanWriteOffs.GetAll;
using ZARI.Application.Features.Loan.LoanWriteOffs.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// DRAFT — the actual GL posting and account status flip happen on Approve, not here, same as
/// every other posting document in this module.
/// </summary>
public sealed class CreateLoanWriteOffCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreateLoanWriteOffCommand, Result<LoanWriteOffResponse>>
{
    public async Task<Result<LoanWriteOffResponse>> HandleAsync(CreateLoanWriteOffCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_WRITE_OFFS", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<LoanWriteOffResponse>(Error.Forbidden("LoanWriteOff.Forbidden", "You do not have permission to create loan write-offs for this branch."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<LoanWriteOffResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var account = await dbContext.LoanAccounts
            .Include(a => a.Customer)
            .Include(a => a.LoanProduct)
            .FirstOrDefaultAsync(a => a.Id == command.LoanAccountId, cancellationToken);
        if (account is null)
            return Result.Failure<LoanWriteOffResponse>(Error.NotFound("LoanAccount.NotFound", $"Loan account with ID '{command.LoanAccountId}' was not found."));

        if (account.Status != "ACTIVE")
            return Result.Failure<LoanWriteOffResponse>(Error.Validation("LoanWriteOff.AccountNotActive", "Only an active loan account can be written off."));

        var alreadyInProgress = await dbContext.LoanWriteOffs.AnyAsync(w => w.LoanAccountId == command.LoanAccountId && w.Status != "CANCELLED", cancellationToken);
        if (alreadyInProgress)
            return Result.Failure<LoanWriteOffResponse>(Error.Conflict("LoanWriteOff.AlreadyExists", "This loan account already has a write-off in progress or posted."));

        var expenseAccountExists = await dbContext.GlAccounts.AnyAsync(g => g.Id == command.WriteOffExpenseAccountId, cancellationToken);
        if (!expenseAccountExists)
            return Result.Failure<LoanWriteOffResponse>(Error.NotFound("GlAccount.NotFound", $"GL account with ID '{command.WriteOffExpenseAccountId}' was not found."));

        var principalBalance = await LoanRestructuringEligibility.GetCurrentPrincipalBalanceAsync(dbContext, command.LoanAccountId, cancellationToken);
        if (principalBalance <= 0)
            return Result.Failure<LoanWriteOffResponse>(Error.Validation("LoanWriteOff.NoBalance", "This loan account has no outstanding principal balance to write off."));

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "LOAN-WO"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<LoanWriteOffResponse>(numberResult.Error!);

        var writeOff = new LoanWriteOff
        {
            WriteOffNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            LoanAccountId = command.LoanAccountId,
            WriteOffDate = command.WriteOffDate,
            Amount = principalBalance,
            WriteOffExpenseAccountId = command.WriteOffExpenseAccountId,
            Reason = command.Reason,
            Status = "DRAFT",
            Remarks = command.Remarks,
            CreatedBy = command.CreatedBy
        };

        dbContext.LoanWriteOffs.Add(writeOff);
        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await dbContext.LoanWriteOffs
            .Include(w => w.LoanAccount).ThenInclude(a => a.Customer)
            .Include(w => w.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(w => w.WriteOffExpenseAccount)
            .FirstAsync(w => w.Id == writeOff.Id, cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_WRITE_OFF", writeOff.Id.ToString(), writeOff.BranchId, "CREATED", "ACTIVITY",
                "created this loan write-off", command.CreatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanWriteOffResponse>(notifyResult.Error!);

        return Result.Success(LoanWriteOffMapper.ToResponse(saved));
    }
}
