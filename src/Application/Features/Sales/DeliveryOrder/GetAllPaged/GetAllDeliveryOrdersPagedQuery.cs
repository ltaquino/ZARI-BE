namespace ZARI.Application.Features.Sales.DeliveryOrders.GetAllPaged;

using ZARI.Application.Features.Sales.DeliveryOrders.GetAll;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllDeliveryOrdersPagedQuery(int Page = 1, int PageSize = 20, string? Search = null) : IQuery<Result<PagedResult<DeliveryOrderResponse>>>;
