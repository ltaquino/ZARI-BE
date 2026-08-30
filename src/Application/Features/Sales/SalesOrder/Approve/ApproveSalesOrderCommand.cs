namespace ZARI.Application.Features.Sales.SalesOrders.Approve;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesOrders.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveSalesOrderCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<SalesOrderResponse>>;
