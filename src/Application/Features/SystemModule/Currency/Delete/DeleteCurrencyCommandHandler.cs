namespace ZARI.Application.Features.SystemModule.Currencies.Delete;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteCurrencyCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteCurrencyCommand>
{
    public async Task<Result> HandleAsync(DeleteCurrencyCommand command, CancellationToken cancellationToken = default)
    {
        var currency = await dbContext.Currencies.FindAsync([command.Id], cancellationToken);
        if (currency is null)
            return Result.Failure(Error.NotFound("Currency.NotFound", $"Currency with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("CURRENCIES", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("Currency.Forbidden", "You do not have permission to delete currencies."));

        var isBaseCurrency = await dbContext.Companies.AnyAsync(c => c.BaseCurrencyId == command.Id, cancellationToken);
        if (isBaseCurrency)
            return Result.Failure(Error.Conflict("Currency.IsBaseCurrency", "Cannot delete this currency — it is the company base currency."));

        var usedByExchangeRates = await dbContext.ExchangeRates.CountAsync(r => r.CurrencyId == command.Id, cancellationToken);
        if (usedByExchangeRates > 0)
            return Result.Failure(Error.Conflict("Currency.HasExchangeRates", $"Cannot delete this currency — it has {usedByExchangeRates} exchange rate record{(usedByExchangeRates == 1 ? "" : "s")}."));

        var usedByBankAccounts = await dbContext.BankAccounts.CountAsync(b => b.CurrencyId == command.Id, cancellationToken);
        if (usedByBankAccounts > 0)
            return Result.Failure(Error.Conflict("Currency.HasBankAccounts", $"Cannot delete this currency — it is used by {usedByBankAccounts} bank account{(usedByBankAccounts == 1 ? "" : "s")}."));

        var usedBySuppliers = await dbContext.Suppliers.CountAsync(s => s.CurrencyId == command.Id, cancellationToken);
        if (usedBySuppliers > 0)
            return Result.Failure(Error.Conflict("Currency.HasSuppliers", $"Cannot delete this currency — it is used by {usedBySuppliers} supplier{(usedBySuppliers == 1 ? "" : "s")}."));

        dbContext.Currencies.Remove(currency);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
