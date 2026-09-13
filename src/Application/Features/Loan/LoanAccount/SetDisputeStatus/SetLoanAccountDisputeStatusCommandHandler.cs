namespace ZARI.Application.Features.Loan.LoanAccounts.SetDisputeStatus;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanAccounts.GetAll;
using ZARI.Application.Features.Loan.LoanAccounts.Shared;
using ZARI.Domain.Common;

/// <summary>
/// Raises or clears the CISA negative-credit-information "pending litigation/dispute" flag — not
/// status-gated like every other LoanAccount mutation, since a dispute can arise or resolve at any
/// point in the account's life regardless of its workflow state.
/// </summary>
public sealed class SetLoanAccountDisputeStatusCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<SetLoanAccountDisputeStatusCommand, Result<LoanAccountResponse>>
{
    public async Task<Result<LoanAccountResponse>> HandleAsync(SetLoanAccountDisputeStatusCommand command, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.LoanAccounts
            .Include(a => a.Customer)
            .Include(a => a.LoanProduct)
            .Include(a => a.LoanApplication)
            .Include(a => a.ScheduleLines)
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken);

        if (account is null)
            return Result.Failure<LoanAccountResponse>(Error.NotFound("LoanAccount.NotFound", $"Loan account with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_ACCOUNTS", FormAction.Edit, account.BranchId, cancellationToken))
            return Result.Failure<LoanAccountResponse>(Error.Forbidden("LoanAccount.Forbidden", "You do not have permission to update this loan account for this branch."));

        account.IsDisputed = command.IsDisputed;
        account.DisputeNotes = command.IsDisputed ? command.DisputeNotes : null;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(LoanAccountMapper.ToResponse(account));
    }
}
