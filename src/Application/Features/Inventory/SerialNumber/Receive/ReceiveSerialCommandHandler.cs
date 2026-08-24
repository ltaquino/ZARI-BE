namespace ZARI.Application.Features.Inventory.SerialNumbers.Receive;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.SerialNumbers.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class ReceiveSerialCommandHandler(IAppDbContext dbContext) : ICommandHandler<ReceiveSerialCommand, Result<SerialNumberResponse>>
{
    public async Task<Result<SerialNumberResponse>> HandleAsync(ReceiveSerialCommand command, CancellationToken cancellationToken = default)
    {
        var itemExists = await dbContext.Items.AnyAsync(i => i.Id == command.ItemId, cancellationToken);
        if (!itemExists)
            return Result.Failure<SerialNumberResponse>(Error.NotFound("Item.NotFound", $"Item with ID '{command.ItemId}' was not found."));

        var warehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.WarehouseId, cancellationToken);
        if (!warehouseExists)
            return Result.Failure<SerialNumberResponse>(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.WarehouseId}' was not found."));

        var existing = await dbContext.SerialNumbers
            .FirstOrDefaultAsync(s => s.ItemId == command.ItemId && s.SerialNo == command.SerialNo, cancellationToken);

        if (existing is not null && existing.Status == "IN_STOCK" && existing.WarehouseId != command.WarehouseId)
        {
            return Result.Failure<SerialNumberResponse>(Error.Validation(
                "SerialNumber.AlreadyInStock",
                $"Serial {command.SerialNo} is already recorded in stock at a different warehouse."));
        }

        if (existing is not null)
        {
            existing.Status = "IN_STOCK";
            existing.WarehouseId = command.WarehouseId;
        }
        else
        {
            existing = new SerialNumber
            {
                ItemId = command.ItemId,
                SerialNo = command.SerialNo,
                WarehouseId = command.WarehouseId,
                Status = "IN_STOCK"
            };
            dbContext.SerialNumbers.Add(existing);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new SerialNumberResponse(existing.Id, existing.ItemId, existing.SerialNo, existing.WarehouseId, existing.Status, existing.CreatedAt));
    }
}
