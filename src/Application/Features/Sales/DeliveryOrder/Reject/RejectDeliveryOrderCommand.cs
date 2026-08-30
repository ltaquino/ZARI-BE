namespace ZARI.Application.Features.Sales.DeliveryOrders.Reject;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.DeliveryOrders.GetAll;
using ZARI.Domain.Common;

public sealed record RejectDeliveryOrderCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<DeliveryOrderResponse>>;
