namespace ZARI.Application.Features.Inventory.Warehouses.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Warehouses.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateWarehouseCommandHandler(IAppDbContext dbContext) : ICommandHandler<CreateWarehouseCommand, Result<WarehouseResponse>>
{
    public async Task<Result<WarehouseResponse>> HandleAsync(CreateWarehouseCommand command, CancellationToken cancellationToken = default)
    {
        var codeExists = await dbContext.Warehouses
            .AnyAsync(w => w.Code == command.Code, cancellationToken);

        if (codeExists)
            return Result.Failure<WarehouseResponse>(Error.Conflict("Warehouse.DuplicateCode", $"A warehouse with code '{command.Code}' already exists."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<WarehouseResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var warehouse = new Warehouse
        {
            BranchId = command.BranchId,
            Code = command.Code,
            Name = command.Name,
            WarehouseType = command.WarehouseType,
            Status = command.Status
        };

        dbContext.Warehouses.Add(warehouse);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new WarehouseResponse(warehouse.Id, warehouse.BranchId, warehouse.Code, warehouse.Name, warehouse.WarehouseType, warehouse.Status, warehouse.CreatedAt);
        return Result.Success(response);
    }
}
