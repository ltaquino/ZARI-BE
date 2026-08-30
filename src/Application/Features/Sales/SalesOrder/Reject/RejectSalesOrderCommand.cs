namespace ZARI.Application.Features.Sales.SalesOrders.Reject;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesOrders.GetAll;
using ZARI.Domain.Common;

public sealed record RejectSalesOrderCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<SalesOrderResponse>>;
