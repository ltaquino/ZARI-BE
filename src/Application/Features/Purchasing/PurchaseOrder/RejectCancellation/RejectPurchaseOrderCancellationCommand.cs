namespace ZARI.Application.Features.Purchasing.PurchaseOrders.RejectCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseOrders.GetAll;
using ZARI.Domain.Common;

public sealed record RejectPurchaseOrderCancellationCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<PurchaseOrderResponse>>;
