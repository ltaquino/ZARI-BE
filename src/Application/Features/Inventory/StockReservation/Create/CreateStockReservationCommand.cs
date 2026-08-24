namespace ZARI.Application.Features.Inventory.StockReservations.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockReservations.GetAll;
using ZARI.Domain.Common;

public sealed record CreateStockReservationCommand(
    Guid ItemId,
    string BranchId,
    Guid WarehouseId,
    decimal QtyReserved,
    DateTimeOffset ReservedDate,
    DateTimeOffset? ExpiryDate,
    string? ReferenceNote) : ICommand<Result<StockReservationResponse>>;
