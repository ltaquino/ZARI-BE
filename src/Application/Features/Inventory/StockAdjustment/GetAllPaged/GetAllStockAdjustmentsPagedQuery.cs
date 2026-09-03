namespace ZARI.Application.Features.Inventory.StockAdjustments.GetAllPaged;

using ZARI.Application.Features.Inventory.StockAdjustments.GetAll;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllStockAdjustmentsPagedQuery(int Page = 1, int PageSize = 20, string? Search = null) : IQuery<Result<PagedResult<StockAdjustmentResponse>>>;
