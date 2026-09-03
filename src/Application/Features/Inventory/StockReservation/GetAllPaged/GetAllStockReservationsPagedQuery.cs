namespace ZARI.Application.Features.Inventory.StockReservations.GetAllPaged;

using ZARI.Application.Features.Inventory.StockReservations.GetAll;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllStockReservationsPagedQuery(int Page = 1, int PageSize = 20, string? Search = null) : IQuery<Result<PagedResult<StockReservationResponse>>>;
