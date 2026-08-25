namespace ZARI.Application.Features.Purchasing.Suppliers.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetSupplierQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetSupplierQuery, Result<SupplierResponse>>
{
    public async Task<Result<SupplierResponse>> HandleAsync(GetSupplierQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("SUPPLIERS", FormAction.View, cancellationToken))
            return Result.Failure<SupplierResponse>(Error.Forbidden("Supplier.Forbidden", "You do not have permission to view suppliers."));

        var supplier = await dbContext.Suppliers
            .Where(s => s.Id == query.Id)
            .Select(s => new SupplierResponse(s.Id, s.Code, s.Name, s.TaxId, s.PaymentTerms, s.CurrencyId, s.ApAccountId,
                s.Address, s.ContactPerson, s.ContactNumber, s.Email, s.Status, s.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (supplier is null)
            return Result.Failure<SupplierResponse>(Error.NotFound("Supplier.NotFound", $"Supplier with ID '{query.Id}' was not found."));

        return Result.Success(supplier);
    }
}
