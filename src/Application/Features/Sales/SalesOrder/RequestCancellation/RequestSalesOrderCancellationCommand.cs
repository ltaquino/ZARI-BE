namespace ZARI.Application.Features.Sales.SalesOrders.RequestCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesOrders.GetAll;
using ZARI.Domain.Common;

public sealed record RequestSalesOrderCancellationCommand(Guid Id, string RequestedBy, string Reason) : ICommand<Result<SalesOrderResponse>>;
