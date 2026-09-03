namespace ZARI.Application.Features.Inventory.StockTransferRequests.GetAllPaged;

using ZARI.Application.Features.Inventory.StockTransferRequests.GetAll;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllStockTransferRequestsPagedQuery(int Page = 1, int PageSize = 20, string? Search = null) : IQuery<Result<PagedResult<StockTransferRequestResponse>>>;
