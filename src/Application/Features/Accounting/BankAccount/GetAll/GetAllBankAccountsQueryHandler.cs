namespace ZARI.Application.Features.Accounting.BankAccounts.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.BankAccounts.Get;
using ZARI.Domain.Common;

public sealed class GetAllBankAccountsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllBankAccountsQuery, Result<List<BankAccountResponse>>>
{
    public async Task<Result<List<BankAccountResponse>>> HandleAsync(GetAllBankAccountsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("BANK_ACCOUNTS", FormAction.View, cancellationToken))
            return Result.Failure<List<BankAccountResponse>>(Error.Forbidden("BankAccount.Forbidden", "You do not have permission to view bank accounts."));

        var items = await dbContext.BankAccounts
            .OrderBy(b => b.AccountName)
            .Select(b => new BankAccountResponse(b.Id, b.BranchId, b.AccountName, b.AccountNumber, b.BankName, b.GlAccountId, b.CurrencyId, b.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
