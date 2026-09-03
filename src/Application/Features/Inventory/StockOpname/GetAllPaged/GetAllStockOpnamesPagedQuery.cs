namespace ZARI.Application.Features.Inventory.StockOpnames.GetAllPaged;

using ZARI.Application.Features.Inventory.StockOpnames.GetAll;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllStockOpnamesPagedQuery(int Page = 1, int PageSize = 20, string? Search = null) : IQuery<Result<PagedResult<StockOpnameResponse>>>;
