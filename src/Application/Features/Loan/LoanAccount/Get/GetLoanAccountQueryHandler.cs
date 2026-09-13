namespace ZARI.Application.Features.Loan.LoanAccounts.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanAccounts.GetAll;
using ZARI.Application.Features.Loan.LoanAccounts.Shared;
using ZARI.Domain.Common;

public sealed class GetLoanAccountQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetLoanAccountQuery, Result<LoanAccountResponse>>
{
    public async Task<Result<LoanAccountResponse>> HandleAsync(GetLoanAccountQuery query, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.LoanAccounts
            .Include(a => a.Customer)
            .Include(a => a.LoanProduct)
            .Include(a => a.LoanApplication)
            .Include(a => a.ScheduleLines)
            .FirstOrDefaultAsync(a => a.Id == query.Id, cancellationToken);

        if (account is null)
            return Result.Failure<LoanAccountResponse>(Error.NotFound("LoanAccount.NotFound", $"Loan account with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_ACCOUNTS", FormAction.View, account.BranchId, cancellationToken))
            return Result.Failure<LoanAccountResponse>(Error.Forbidden("LoanAccount.Forbidden", "You do not have permission to view loan accounts for this branch."));

        return Result.Success(LoanAccountMapper.ToResponse(account));
    }
}
