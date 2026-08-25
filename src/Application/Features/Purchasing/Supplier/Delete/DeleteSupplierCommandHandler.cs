namespace ZARI.Application.Features.Purchasing.Suppliers.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteSupplierCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteSupplierCommand>
{
    public async Task<Result> HandleAsync(DeleteSupplierCommand command, CancellationToken cancellationToken = default)
    {
        var supplier = await dbContext.Suppliers.FindAsync([command.Id], cancellationToken);
        if (supplier is null)
            return Result.Failure(Error.NotFound("Supplier.NotFound", $"Supplier with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("SUPPLIERS", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("Supplier.Forbidden", "You do not have permission to delete suppliers."));

        dbContext.Suppliers.Remove(supplier);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
