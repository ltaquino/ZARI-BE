namespace ZARI.Application.Features.Inventory.Warehouses.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateWarehouseCommandHandler(IAppDbContext dbContext) : ICommandHandler<UpdateWarehouseCommand>
{
    public async Task<Result> HandleAsync(UpdateWarehouseCommand command, CancellationToken cancellationToken = default)
    {
        var warehouse = await dbContext.Warehouses.FindAsync([command.Id], cancellationToken);
        if (warehouse is null)
            return Result.Failure(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.Id}' was not found."));

        var duplicateCode = await dbContext.Warehouses
            .AnyAsync(w => w.Id != command.Id && w.Code == command.Code, cancellationToken);

        if (duplicateCode)
            return Result.Failure(Error.Conflict("Warehouse.DuplicateCode", $"A warehouse with code '{command.Code}' already exists."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        warehouse.BranchId = command.BranchId;
        warehouse.Code = command.Code;
        warehouse.Name = command.Name;
        warehouse.WarehouseType = command.WarehouseType;
        warehouse.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
