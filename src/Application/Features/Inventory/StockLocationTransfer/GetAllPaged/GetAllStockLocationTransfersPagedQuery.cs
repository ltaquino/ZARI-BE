namespace ZARI.Application.Features.Inventory.StockLocationTransfers.GetAllPaged;

using ZARI.Application.Features.Inventory.StockLocationTransfers.GetAll;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllStockLocationTransfersPagedQuery(int Page = 1, int PageSize = 20, string? Search = null) : IQuery<Result<PagedResult<StockLocationTransferResponse>>>;
