namespace ZARI.Application.Features.Accounting.BankAccounts.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.BankAccounts.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateBankAccountCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<CreateBankAccountCommand, Result<BankAccountResponse>>
{
    public async Task<Result<BankAccountResponse>> HandleAsync(CreateBankAccountCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("BANK_ACCOUNTS", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<BankAccountResponse>(Error.Forbidden("BankAccount.Forbidden", "You do not have permission to create bank accounts for this branch."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<BankAccountResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var glAccountExists = await dbContext.GlAccounts.AnyAsync(a => a.Id == command.GlAccountId, cancellationToken);
        if (!glAccountExists)
            return Result.Failure<BankAccountResponse>(Error.NotFound("GlAccount.NotFound", $"GL account with ID '{command.GlAccountId}' was not found."));

        if (command.CurrencyId is not null)
        {
            var currencyExists = await dbContext.Currencies.AnyAsync(c => c.Id == command.CurrencyId, cancellationToken);
            if (!currencyExists)
                return Result.Failure<BankAccountResponse>(Error.NotFound("Currency.NotFound", $"Currency with ID '{command.CurrencyId}' was not found."));
        }

        var bankAccount = new BankAccount
        {
            BranchId = command.BranchId,
            AccountName = command.AccountName,
            AccountNumber = command.AccountNumber,
            BankName = command.BankName,
            GlAccountId = command.GlAccountId,
            CurrencyId = command.CurrencyId
        };

        dbContext.BankAccounts.Add(bankAccount);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new BankAccountResponse(bankAccount.Id, bankAccount.BranchId, bankAccount.AccountName, bankAccount.AccountNumber, bankAccount.BankName, bankAccount.GlAccountId, bankAccount.CurrencyId, bankAccount.CreatedAt);
        return Result.Success(response);
    }
}
