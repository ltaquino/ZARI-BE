namespace ZARI.Application.Features.Purchasing.Suppliers.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.Suppliers.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateSupplierCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<CreateSupplierCommand, Result<SupplierResponse>>
{
    public async Task<Result<SupplierResponse>> HandleAsync(CreateSupplierCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("SUPPLIERS", FormAction.Create, cancellationToken))
            return Result.Failure<SupplierResponse>(Error.Forbidden("Supplier.Forbidden", "You do not have permission to create suppliers."));

        var codeExists = await dbContext.Suppliers.AnyAsync(s => s.Code == command.Code, cancellationToken);
        if (codeExists)
            return Result.Failure<SupplierResponse>(Error.Conflict("Supplier.DuplicateCode", $"A supplier with code '{command.Code}' already exists."));

        if (command.CurrencyId is not null)
        {
            var currencyExists = await dbContext.Currencies.AnyAsync(c => c.Id == command.CurrencyId, cancellationToken);
            if (!currencyExists)
                return Result.Failure<SupplierResponse>(Error.NotFound("Currency.NotFound", $"Currency with ID '{command.CurrencyId}' was not found."));
        }

        if (command.ApAccountId is not null)
        {
            var glAccountExists = await dbContext.GlAccounts.AnyAsync(a => a.Id == command.ApAccountId, cancellationToken);
            if (!glAccountExists)
                return Result.Failure<SupplierResponse>(Error.NotFound("GlAccount.NotFound", $"GL account with ID '{command.ApAccountId}' was not found."));
        }

        var supplier = new Supplier
        {
            Code = command.Code,
            Name = command.Name,
            TaxId = command.TaxId,
            PaymentTerms = command.PaymentTerms,
            CurrencyId = command.CurrencyId,
            ApAccountId = command.ApAccountId,
            Address = command.Address,
            ContactPerson = command.ContactPerson,
            ContactNumber = command.ContactNumber,
            Email = command.Email,
            Status = command.Status
        };

        dbContext.Suppliers.Add(supplier);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new SupplierResponse(supplier.Id, supplier.Code, supplier.Name, supplier.TaxId, supplier.PaymentTerms,
            supplier.CurrencyId, supplier.ApAccountId, supplier.Address, supplier.ContactPerson, supplier.ContactNumber,
            supplier.Email, supplier.Status, supplier.CreatedAt);
        return Result.Success(response);
    }
}
