namespace ZARI.Application.Features.Purchasing.PurchaseOrders.GetAllPaged;

using ZARI.Application.Features.Purchasing.PurchaseOrders.GetAll;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllPurchaseOrdersPagedQuery(int Page = 1, int PageSize = 20, string? Search = null) : IQuery<Result<PagedResult<PurchaseOrderResponse>>>;
