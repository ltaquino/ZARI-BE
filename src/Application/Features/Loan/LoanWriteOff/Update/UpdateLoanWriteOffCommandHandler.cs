namespace ZARI.Application.Features.Loan.LoanWriteOffs.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanRestructurings.Shared;
using ZARI.Application.Features.Loan.LoanWriteOffs.GetAll;
using ZARI.Application.Features.Loan.LoanWriteOffs.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>Only while DRAFT. The loan account a write-off is against never changes once created.</summary>
public sealed class UpdateLoanWriteOffCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<UpdateLoanWriteOffCommand, Result<LoanWriteOffResponse>>
{
    public async Task<Result<LoanWriteOffResponse>> HandleAsync(UpdateLoanWriteOffCommand command, CancellationToken cancellationToken = default)
    {
        var writeOff = await dbContext.LoanWriteOffs.FirstOrDefaultAsync(w => w.Id == command.Id, cancellationToken);

        if (writeOff is null)
            return Result.Failure<LoanWriteOffResponse>(Error.NotFound("LoanWriteOff.NotFound", $"Loan write-off with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_WRITE_OFFS", FormAction.Edit, writeOff.BranchId, cancellationToken))
            return Result.Failure<LoanWriteOffResponse>(Error.Forbidden("LoanWriteOff.Forbidden", "You do not have permission to edit this loan write-off for this branch."));

        if (writeOff.Status != "DRAFT")
            return Result.Failure<LoanWriteOffResponse>(Error.Validation("LoanWriteOff.NotDraft", "Only a draft loan write-off can be edited."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<LoanWriteOffResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var expenseAccountExists = await dbContext.GlAccounts.AnyAsync(g => g.Id == command.WriteOffExpenseAccountId, cancellationToken);
        if (!expenseAccountExists)
            return Result.Failure<LoanWriteOffResponse>(Error.NotFound("GlAccount.NotFound", $"GL account with ID '{command.WriteOffExpenseAccountId}' was not found."));

        writeOff.BranchId = command.BranchId;
        writeOff.WriteOffDate = command.WriteOffDate;
        writeOff.Amount = await LoanRestructuringEligibility.GetCurrentPrincipalBalanceAsync(dbContext, writeOff.LoanAccountId, cancellationToken);
        writeOff.WriteOffExpenseAccountId = command.WriteOffExpenseAccountId;
        writeOff.Reason = command.Reason;
        writeOff.Remarks = command.Remarks;
        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await dbContext.LoanWriteOffs
            .Include(w => w.LoanAccount).ThenInclude(a => a.Customer)
            .Include(w => w.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(w => w.WriteOffExpenseAccount)
            .FirstAsync(w => w.Id == writeOff.Id, cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_WRITE_OFF", writeOff.Id.ToString(), writeOff.BranchId, "UPDATED", "ACTIVITY",
                "updated this loan write-off", command.UpdatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanWriteOffResponse>(notifyResult.Error!);

        return Result.Success(LoanWriteOffMapper.ToResponse(saved));
    }
}
