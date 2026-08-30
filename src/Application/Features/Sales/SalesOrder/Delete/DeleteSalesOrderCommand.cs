namespace ZARI.Application.Features.Sales.SalesOrders.Delete;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record DeleteSalesOrderCommand(Guid Id) : ICommand<Result>;
