namespace ZARI.Application.Features.Sales.DeliveryOrders.ApproveCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.DeliveryOrders.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveDeliveryOrderCancellationCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<DeliveryOrderResponse>>;
