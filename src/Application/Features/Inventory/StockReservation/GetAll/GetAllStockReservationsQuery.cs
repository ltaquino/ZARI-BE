namespace ZARI.Application.Features.Inventory.StockReservations.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllStockReservationsQuery : IQuery<Result<List<StockReservationResponse>>>;

public sealed record StockReservationResponse(
    Guid Id,
    Guid ItemId,
    string BranchId,
    Guid WarehouseId,
    decimal QtyReserved,
    DateTimeOffset ReservedDate,
    DateTimeOffset? ExpiryDate,
    string? ReferenceNote,
    string Status,
    string? ReleasedBy,
    DateTimeOffset? ReleasedAt,
    DateTimeOffset CreatedAt);
