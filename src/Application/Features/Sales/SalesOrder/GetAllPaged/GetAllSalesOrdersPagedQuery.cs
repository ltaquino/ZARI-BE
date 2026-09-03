namespace ZARI.Application.Features.Sales.SalesOrders.GetAllPaged;

using ZARI.Application.Features.Sales.SalesOrders.GetAll;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllSalesOrdersPagedQuery(int Page = 1, int PageSize = 20, string? Search = null) : IQuery<Result<PagedResult<SalesOrderResponse>>>;
