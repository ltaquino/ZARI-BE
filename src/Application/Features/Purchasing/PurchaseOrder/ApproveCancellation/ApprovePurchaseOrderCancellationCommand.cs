namespace ZARI.Application.Features.Purchasing.PurchaseOrders.ApproveCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseOrders.GetAll;
using ZARI.Domain.Common;

public sealed record ApprovePurchaseOrderCancellationCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<PurchaseOrderResponse>>;
