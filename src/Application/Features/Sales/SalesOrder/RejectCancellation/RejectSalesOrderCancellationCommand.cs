namespace ZARI.Application.Features.Sales.SalesOrders.RejectCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesOrders.GetAll;
using ZARI.Domain.Common;

public sealed record RejectSalesOrderCancellationCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<SalesOrderResponse>>;
