namespace ZARI.Application.Features.Accounting.BankAccounts.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateBankAccountCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<UpdateBankAccountCommand>
{
    public async Task<Result> HandleAsync(UpdateBankAccountCommand command, CancellationToken cancellationToken = default)
    {
        var bankAccount = await dbContext.BankAccounts.FindAsync([command.Id], cancellationToken);
        if (bankAccount is null)
            return Result.Failure(Error.NotFound("BankAccount.NotFound", $"Bank account with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("BANK_ACCOUNTS", FormAction.Edit, bankAccount.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("BankAccount.Forbidden", "You do not have permission to update bank accounts for this branch."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var glAccountExists = await dbContext.GlAccounts.AnyAsync(a => a.Id == command.GlAccountId, cancellationToken);
        if (!glAccountExists)
            return Result.Failure(Error.NotFound("GlAccount.NotFound", $"GL account with ID '{command.GlAccountId}' was not found."));

        if (command.CurrencyId is not null)
        {
            var currencyExists = await dbContext.Currencies.AnyAsync(c => c.Id == command.CurrencyId, cancellationToken);
            if (!currencyExists)
                return Result.Failure(Error.NotFound("Currency.NotFound", $"Currency with ID '{command.CurrencyId}' was not found."));
        }

        bankAccount.BranchId = command.BranchId;
        bankAccount.AccountName = command.AccountName;
        bankAccount.AccountNumber = command.AccountNumber;
        bankAccount.BankName = command.BankName;
        bankAccount.GlAccountId = command.GlAccountId;
        bankAccount.CurrencyId = command.CurrencyId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
