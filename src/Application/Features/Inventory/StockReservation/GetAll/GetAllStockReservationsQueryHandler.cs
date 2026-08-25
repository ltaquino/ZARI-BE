namespace ZARI.Application.Features.Inventory.StockReservations.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetAllStockReservationsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllStockReservationsQuery, Result<List<StockReservationResponse>>>
{
    public async Task<Result<List<StockReservationResponse>>> HandleAsync(GetAllStockReservationsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("STOCK_RESERVATIONS", FormAction.View, cancellationToken))
            return Result.Failure<List<StockReservationResponse>>(Error.Forbidden("StockReservation.Forbidden", "You do not have permission to view stock reservations."));

        var items = await dbContext.StockReservations
            .OrderByDescending(r => r.ReservedDate)
            .Select(r => new StockReservationResponse(
                r.Id, r.ItemId, r.BranchId, r.WarehouseId, r.QtyReserved, r.ReservedDate, r.ExpiryDate,
                r.ReferenceNote, r.Status, r.ReleasedBy, r.ReleasedAt, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
