namespace ZARI.Application.Features.Inventory.StockReservations.GetAllPaged;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockReservations.GetAll;
using ZARI.Domain.Common;

public sealed class GetAllStockReservationsPagedQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllStockReservationsPagedQuery, Result<PagedResult<StockReservationResponse>>>
{
    public async Task<Result<PagedResult<StockReservationResponse>>> HandleAsync(GetAllStockReservationsPagedQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("STOCK_RESERVATIONS", FormAction.View, cancellationToken))
            return Result.Failure<PagedResult<StockReservationResponse>>(Error.Forbidden("StockReservation.Forbidden", "You do not have permission to view stock reservations."));

        var baseQuery = dbContext.StockReservations.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(x => x.ReferenceNote != null && x.ReferenceNote.Contains(query.Search));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .OrderByDescending(r => r.ReservedDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new StockReservationResponse(
                r.Id, r.ItemId, r.BranchId, r.WarehouseId, r.QtyReserved, r.ReservedDate, r.ExpiryDate,
                r.ReferenceNote, r.Status, r.ReleasedBy, r.ReleasedAt, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<StockReservationResponse>(items, totalCount, query.Page, query.PageSize));
    }
}
