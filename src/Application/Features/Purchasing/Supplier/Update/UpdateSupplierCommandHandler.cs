namespace ZARI.Application.Features.Purchasing.Suppliers.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateSupplierCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<UpdateSupplierCommand>
{
    public async Task<Result> HandleAsync(UpdateSupplierCommand command, CancellationToken cancellationToken = default)
    {
        var supplier = await dbContext.Suppliers.FindAsync([command.Id], cancellationToken);
        if (supplier is null)
            return Result.Failure(Error.NotFound("Supplier.NotFound", $"Supplier with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("SUPPLIERS", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("Supplier.Forbidden", "You do not have permission to update suppliers."));

        var duplicateCode = await dbContext.Suppliers
            .AnyAsync(s => s.Id != command.Id && s.Code == command.Code, cancellationToken);

        if (duplicateCode)
            return Result.Failure(Error.Conflict("Supplier.DuplicateCode", $"A supplier with code '{command.Code}' already exists."));

        if (command.CurrencyId is not null)
        {
            var currencyExists = await dbContext.Currencies.AnyAsync(c => c.Id == command.CurrencyId, cancellationToken);
            if (!currencyExists)
                return Result.Failure(Error.NotFound("Currency.NotFound", $"Currency with ID '{command.CurrencyId}' was not found."));
        }

        if (command.ApAccountId is not null)
        {
            var glAccountExists = await dbContext.GlAccounts.AnyAsync(a => a.Id == command.ApAccountId, cancellationToken);
            if (!glAccountExists)
                return Result.Failure(Error.NotFound("GlAccount.NotFound", $"GL account with ID '{command.ApAccountId}' was not found."));
        }

        supplier.Code = command.Code;
        supplier.Name = command.Name;
        supplier.TaxId = command.TaxId;
        supplier.PaymentTermsDays = command.PaymentTermsDays;
        supplier.CurrencyId = command.CurrencyId;
        supplier.ApAccountId = command.ApAccountId;
        supplier.Address = command.Address;
        supplier.ContactPerson = command.ContactPerson;
        supplier.ContactNumber = command.ContactNumber;
        supplier.Email = command.Email;
        supplier.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
