namespace ZARI.Application.Features.Loan.LoanAccounts.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteLoanAccountCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteLoanAccountCommand>
{
    public async Task<Result> HandleAsync(DeleteLoanAccountCommand command, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.LoanAccounts.FindAsync([command.Id], cancellationToken);
        if (account is null)
            return Result.Failure(Error.NotFound("LoanAccount.NotFound", $"Loan account with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_ACCOUNTS", FormAction.Delete, account.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("LoanAccount.Forbidden", "You do not have permission to delete this loan account for this branch."));

        if (account.Status != "PENDING_DISBURSEMENT")
            return Result.Failure(Error.Validation("LoanAccount.NotDeletable", "Only a loan account still pending disbursement can be deleted — cancel it instead."));

        dbContext.LoanAccounts.Remove(account);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
