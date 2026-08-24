namespace ZARI.Application.Features.SystemModule.Branches.Delete;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteBranchCommandHandler(IAppDbContext dbContext) : ICommandHandler<DeleteBranchCommand>
{
    public async Task<Result> HandleAsync(DeleteBranchCommand command, CancellationToken cancellationToken = default)
    {
        var branch = await dbContext.Branches.FindAsync([command.Id], cancellationToken);
        if (branch is null)
            return Result.Failure(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.Id}' was not found."));

        var warehouseCount = await dbContext.Warehouses.CountAsync(w => w.BranchId == command.Id, cancellationToken);
        if (warehouseCount > 0)
            return Result.Failure(Error.Conflict("Branch.HasWarehouses", $"Cannot delete this branch — it has {warehouseCount} warehouse{(warehouseCount == 1 ? "" : "s")}."));

        var customerCount = await dbContext.Customers.CountAsync(c => c.BranchId == command.Id, cancellationToken);
        if (customerCount > 0)
            return Result.Failure(Error.Conflict("Branch.HasCustomers", $"Cannot delete this branch — it has {customerCount} customer record{(customerCount == 1 ? "" : "s")}."));

        dbContext.Branches.Remove(branch);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
