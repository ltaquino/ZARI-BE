namespace ZARI.Application.Features.SystemModule.Currencies.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.SystemModule.Currencies.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateCurrencyCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<CreateCurrencyCommand, Result<CurrencyResponse>>
{
    public async Task<Result<CurrencyResponse>> HandleAsync(CreateCurrencyCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("CURRENCIES", FormAction.Create, cancellationToken))
            return Result.Failure<CurrencyResponse>(Error.Forbidden("Currency.Forbidden", "You do not have permission to create currencies."));

        var codeExists = await dbContext.Currencies.AnyAsync(c => c.Code == command.Code, cancellationToken);
        if (codeExists)
            return Result.Failure<CurrencyResponse>(Error.Conflict("Currency.DuplicateCode", $"A currency with code '{command.Code}' already exists."));

        var currency = new Currency
        {
            Id = $"cur-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            Code = command.Code,
            Name = command.Name,
            Status = command.Status,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Currencies.Add(currency);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new CurrencyResponse(currency.Id, currency.Code, currency.Name, currency.Status, currency.CreatedAt);
        return Result.Success(response);
    }
}
