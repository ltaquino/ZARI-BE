namespace ZARI.Application.Features.Sales.SalesOrders.Cancel;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesOrders.GetAll;
using ZARI.Domain.Common;

public sealed record CancelSalesOrderCommand(Guid Id, string CancelledBy, string Reason) : ICommand<Result<SalesOrderResponse>>;
