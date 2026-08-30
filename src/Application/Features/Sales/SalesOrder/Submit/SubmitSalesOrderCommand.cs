namespace ZARI.Application.Features.Sales.SalesOrders.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesOrders.GetAll;
using ZARI.Domain.Common;

public sealed record SubmitSalesOrderCommand(Guid Id, string RequestedBy) : ICommand<Result<SalesOrderResponse>>;
