namespace ZARI.Application.Features.Sales.SalesReturns.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteSalesReturnCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteSalesReturnCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteSalesReturnCommand command, CancellationToken cancellationToken = default)
    {
        var salesReturn = await dbContext.SalesReturns.FindAsync([command.Id], cancellationToken);
        if (salesReturn is null)
            return Result.Failure(Error.NotFound("SalesReturn.NotFound", $"Sales return with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("SALES_RETURNS", FormAction.Delete, salesReturn.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("SalesReturn.Forbidden", "You do not have permission to delete sales returns for this branch."));

        if (salesReturn.Status != "DRAFT")
            return Result.Failure(Error.Validation("SalesReturn.NotDraft", "Only draft sales returns can be deleted — cancel it instead."));

        dbContext.SalesReturns.Remove(salesReturn);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
