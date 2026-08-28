namespace ZARI.Application.Features.Purchasing.Suppliers.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.Suppliers.Get;
using ZARI.Domain.Common;

public sealed class GetAllSuppliersQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllSuppliersQuery, Result<List<SupplierResponse>>>
{
    public async Task<Result<List<SupplierResponse>>> HandleAsync(GetAllSuppliersQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("SUPPLIERS", FormAction.View, cancellationToken))
            return Result.Failure<List<SupplierResponse>>(Error.Forbidden("Supplier.Forbidden", "You do not have permission to view suppliers."));

        var items = await dbContext.Suppliers
            .OrderBy(s => s.Name)
            .Select(s => new SupplierResponse(s.Id, s.Code, s.Name, s.TaxId, s.PaymentTermsDays, s.CurrencyId, s.ApAccountId,
                s.Address, s.ContactPerson, s.ContactNumber, s.Email, s.Status, s.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
