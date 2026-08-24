namespace ZARI.Application.Features.Inventory.StockReservations.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetAllStockReservationsQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetAllStockReservationsQuery, Result<List<StockReservationResponse>>>
{
    public async Task<Result<List<StockReservationResponse>>> HandleAsync(GetAllStockReservationsQuery query, CancellationToken cancellationToken = default)
    {
        var items = await dbContext.StockReservations
            .OrderByDescending(r => r.ReservedDate)
            .Select(r => new StockReservationResponse(
                r.Id, r.ItemId, r.BranchId, r.WarehouseId, r.QtyReserved, r.ReservedDate, r.ExpiryDate,
                r.ReferenceNote, r.Status, r.ReleasedBy, r.ReleasedAt, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
