namespace ZARI.Application.Features.Sales.SalesOrders.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesOrders.GetAll;
using ZARI.Domain.Common;

public sealed record GetSalesOrderQuery(Guid Id) : IQuery<Result<SalesOrderResponse>>;
