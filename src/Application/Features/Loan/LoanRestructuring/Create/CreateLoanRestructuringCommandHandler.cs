namespace ZARI.Application.Features.Loan.LoanRestructurings.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanRestructurings.GetAll;
using ZARI.Application.Features.Loan.LoanRestructurings.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// DRAFT — the actual principal roll-forward (new LoanAccount, new schedule, old schedule
/// superseded) happens on Approve, not here, same as LoanDisbursement's Create/Approve split.
/// </summary>
public sealed class CreateLoanRestructuringCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreateLoanRestructuringCommand, Result<LoanRestructuringResponse>>
{
    public async Task<Result<LoanRestructuringResponse>> HandleAsync(CreateLoanRestructuringCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_RESTRUCTURINGS", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<LoanRestructuringResponse>(Error.Forbidden("LoanRestructuring.Forbidden", "You do not have permission to create loan restructurings for this branch."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<LoanRestructuringResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var account = await dbContext.LoanAccounts
            .Include(a => a.Customer)
            .Include(a => a.LoanProduct)
            .Include(a => a.ScheduleLines)
            .FirstOrDefaultAsync(a => a.Id == command.OldLoanAccountId, cancellationToken);
        if (account is null)
            return Result.Failure<LoanRestructuringResponse>(Error.NotFound("LoanAccount.NotFound", $"Loan account with ID '{command.OldLoanAccountId}' was not found."));

        if (account.Status != "ACTIVE")
            return Result.Failure<LoanRestructuringResponse>(Error.Validation("LoanRestructuring.AccountNotActive", "Only an active loan account can be restructured."));

        var alreadyInProgress = await dbContext.LoanRestructurings.AnyAsync(r => r.OldLoanAccountId == command.OldLoanAccountId && r.Status != "CANCELLED", cancellationToken);
        if (alreadyInProgress)
            return Result.Failure<LoanRestructuringResponse>(Error.Conflict("LoanRestructuring.AlreadyExists", "This loan account already has a restructuring in progress or posted."));

        var unpaidArrears = LoanRestructuringEligibility.ComputeUnpaidInterestAndPenalty(account, command.RestructureDate);
        if (unpaidArrears > 0.01m)
        {
            return Result.Failure<LoanRestructuringResponse>(Error.Validation(
                "LoanRestructuring.ArrearsMustBeSettled",
                $"This account still has {unpaidArrears:N2} of unpaid interest/penalty — settle it with a loan payment first. Restructuring only rolls forward the outstanding principal."));
        }

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "LOAN-RESTR"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<LoanRestructuringResponse>(numberResult.Error!);

        var restructuring = new LoanRestructuring
        {
            RestructuringNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            OldLoanAccountId = command.OldLoanAccountId,
            RestructureDate = command.RestructureDate,
            OldPrincipalBalance = await LoanRestructuringEligibility.GetCurrentPrincipalBalanceAsync(dbContext, command.OldLoanAccountId, cancellationToken),
            NewAnnualInterestRatePct = command.NewAnnualInterestRatePct,
            NewTermMonths = command.NewTermMonths,
            NewRepaymentFrequency = command.NewRepaymentFrequency,
            NewGracePeriodDays = command.NewGracePeriodDays,
            NewPenaltyRatePct = command.NewPenaltyRatePct,
            NewFirstDueDate = command.NewFirstDueDate,
            Reason = command.Reason,
            Status = "DRAFT",
            Remarks = command.Remarks,
            CreatedBy = command.CreatedBy
        };

        dbContext.LoanRestructurings.Add(restructuring);
        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await dbContext.LoanRestructurings
            .Include(r => r.OldLoanAccount).ThenInclude(a => a.Customer)
            .Include(r => r.OldLoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(r => r.NewLoanAccount)
            .FirstAsync(r => r.Id == restructuring.Id, cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_RESTRUCTURING", restructuring.Id.ToString(), restructuring.BranchId, "CREATED", "ACTIVITY",
                "created this loan restructuring", command.CreatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanRestructuringResponse>(notifyResult.Error!);

        return Result.Success(LoanRestructuringMapper.ToResponse(saved));
    }
}
