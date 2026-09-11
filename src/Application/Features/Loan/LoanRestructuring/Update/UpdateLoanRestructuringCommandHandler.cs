namespace ZARI.Application.Features.Loan.LoanRestructurings.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanRestructurings.GetAll;
using ZARI.Application.Features.Loan.LoanRestructurings.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>Only while DRAFT. The old loan account a restructuring is against never changes once created — only the new terms do.</summary>
public sealed class UpdateLoanRestructuringCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<UpdateLoanRestructuringCommand, Result<LoanRestructuringResponse>>
{
    public async Task<Result<LoanRestructuringResponse>> HandleAsync(UpdateLoanRestructuringCommand command, CancellationToken cancellationToken = default)
    {
        var restructuring = await dbContext.LoanRestructurings
            .Include(r => r.OldLoanAccount).ThenInclude(a => a.ScheduleLines)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (restructuring is null)
            return Result.Failure<LoanRestructuringResponse>(Error.NotFound("LoanRestructuring.NotFound", $"Loan restructuring with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_RESTRUCTURINGS", FormAction.Edit, restructuring.BranchId, cancellationToken))
            return Result.Failure<LoanRestructuringResponse>(Error.Forbidden("LoanRestructuring.Forbidden", "You do not have permission to edit this loan restructuring for this branch."));

        if (restructuring.Status != "DRAFT")
            return Result.Failure<LoanRestructuringResponse>(Error.Validation("LoanRestructuring.NotDraft", "Only a draft loan restructuring can be edited."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<LoanRestructuringResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var unpaidArrears = LoanRestructuringEligibility.ComputeUnpaidInterestAndPenalty(restructuring.OldLoanAccount, command.RestructureDate);
        if (unpaidArrears > 0.01m)
        {
            return Result.Failure<LoanRestructuringResponse>(Error.Validation(
                "LoanRestructuring.ArrearsMustBeSettled",
                $"This account still has {unpaidArrears:N2} of unpaid interest/penalty — settle it with a loan payment first. Restructuring only rolls forward the outstanding principal."));
        }

        restructuring.BranchId = command.BranchId;
        restructuring.RestructureDate = command.RestructureDate;
        restructuring.OldPrincipalBalance = await LoanRestructuringEligibility.GetCurrentPrincipalBalanceAsync(dbContext, restructuring.OldLoanAccountId, cancellationToken);
        restructuring.NewAnnualInterestRatePct = command.NewAnnualInterestRatePct;
        restructuring.NewTermMonths = command.NewTermMonths;
        restructuring.NewRepaymentFrequency = command.NewRepaymentFrequency;
        restructuring.NewGracePeriodDays = command.NewGracePeriodDays;
        restructuring.NewPenaltyRatePct = command.NewPenaltyRatePct;
        restructuring.NewFirstDueDate = command.NewFirstDueDate;
        restructuring.Reason = command.Reason;
        restructuring.Remarks = command.Remarks;
        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await dbContext.LoanRestructurings
            .Include(r => r.OldLoanAccount).ThenInclude(a => a.Customer)
            .Include(r => r.OldLoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(r => r.NewLoanAccount)
            .FirstAsync(r => r.Id == restructuring.Id, cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_RESTRUCTURING", restructuring.Id.ToString(), restructuring.BranchId, "UPDATED", "ACTIVITY",
                "updated this loan restructuring", command.UpdatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanRestructuringResponse>(notifyResult.Error!);

        return Result.Success(LoanRestructuringMapper.ToResponse(saved));
    }
}
