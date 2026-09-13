namespace ZARI.Application.Features.Loan.LoanAccounts.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanAccounts.Shared;
using ZARI.Domain.Common;

public sealed class GetAllLoanAccountsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllLoanAccountsQuery, Result<List<LoanAccountResponse>>>
{
    public async Task<Result<List<LoanAccountResponse>>> HandleAsync(GetAllLoanAccountsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("LOAN_ACCOUNTS", FormAction.View, cancellationToken))
            return Result.Failure<List<LoanAccountResponse>>(Error.Forbidden("LoanAccount.Forbidden", "You do not have permission to view loan accounts."));

        var accounts = await dbContext.LoanAccounts.AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.LoanProduct)
            .Include(a => a.LoanApplication)
            .Include(a => a.ScheduleLines)
            .OrderByDescending(a => a.GrantDate)
            .ToListAsync(cancellationToken);

        return Result.Success(accounts.Select(LoanAccountMapper.ToResponse).ToList());
    }
}
