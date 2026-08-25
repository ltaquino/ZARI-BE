namespace ZARI.Application.Features.SystemModule.Currencies.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateCurrencyCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<UpdateCurrencyCommand>
{
    public async Task<Result> HandleAsync(UpdateCurrencyCommand command, CancellationToken cancellationToken = default)
    {
        var currency = await dbContext.Currencies.FindAsync([command.Id], cancellationToken);
        if (currency is null)
            return Result.Failure(Error.NotFound("Currency.NotFound", $"Currency with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("CURRENCIES", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("Currency.Forbidden", "You do not have permission to update currencies."));

        var duplicateCode = await dbContext.Currencies
            .AnyAsync(c => c.Id != command.Id && c.Code == command.Code, cancellationToken);

        if (duplicateCode)
            return Result.Failure(Error.Conflict("Currency.DuplicateCode", $"A currency with code '{command.Code}' already exists."));

        currency.Code = command.Code;
        currency.Name = command.Name;
        currency.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
