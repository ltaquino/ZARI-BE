namespace ZARI.Application.Features.Accounting.BankAccounts.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetBankAccountQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetBankAccountQuery, Result<BankAccountResponse>>
{
    public async Task<Result<BankAccountResponse>> HandleAsync(GetBankAccountQuery query, CancellationToken cancellationToken = default)
    {
        var bankAccount = await dbContext.BankAccounts
            .Where(b => b.Id == query.Id)
            .Select(b => new BankAccountResponse(b.Id, b.BranchId, b.AccountName, b.AccountNumber, b.BankName, b.GlAccountId, b.CurrencyId, b.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (bankAccount is null)
            return Result.Failure<BankAccountResponse>(Error.NotFound("BankAccount.NotFound", $"Bank account with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("BANK_ACCOUNTS", FormAction.View, bankAccount.BranchId, cancellationToken))
            return Result.Failure<BankAccountResponse>(Error.Forbidden("BankAccount.Forbidden", "You do not have permission to view bank accounts for this branch."));

        return Result.Success(bankAccount);
    }
}
