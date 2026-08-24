namespace ZARI.Application.Features.Inventory.StockReservations.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockReservations.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateStockReservationCommandHandler(IAppDbContext dbContext) : ICommandHandler<CreateStockReservationCommand, Result<StockReservationResponse>>
{
    public async Task<Result<StockReservationResponse>> HandleAsync(CreateStockReservationCommand command, CancellationToken cancellationToken = default)
    {
        var itemExists = await dbContext.Items.AnyAsync(i => i.Id == command.ItemId, cancellationToken);
        if (!itemExists)
            return Result.Failure<StockReservationResponse>(Error.NotFound("Item.NotFound", $"Item with ID '{command.ItemId}' was not found."));

        var warehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.WarehouseId, cancellationToken);
        if (!warehouseExists)
            return Result.Failure<StockReservationResponse>(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.WarehouseId}' was not found."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<StockReservationResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var reservation = new StockReservation
        {
            ItemId = command.ItemId,
            BranchId = command.BranchId,
            WarehouseId = command.WarehouseId,
            QtyReserved = command.QtyReserved,
            ReservedDate = command.ReservedDate,
            ExpiryDate = command.ExpiryDate,
            ReferenceNote = command.ReferenceNote,
            Status = "ACTIVE"
        };

        dbContext.StockReservations.Add(reservation);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new StockReservationResponse(
            reservation.Id, reservation.ItemId, reservation.BranchId, reservation.WarehouseId, reservation.QtyReserved,
            reservation.ReservedDate, reservation.ExpiryDate, reservation.ReferenceNote, reservation.Status,
            reservation.ReleasedBy, reservation.ReleasedAt, reservation.CreatedAt);

        return Result.Success(response);
    }
}
