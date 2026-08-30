namespace ZARI.Application.Features.Sales.DeliveryOrders.Approve;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.DeliveryOrders.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveDeliveryOrderCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<DeliveryOrderResponse>>;
