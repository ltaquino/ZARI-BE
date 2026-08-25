namespace ZARI.Application.Features.Purchasing.PurchaseOrders.Reject;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseOrders.GetAll;
using ZARI.Domain.Common;

public sealed record RejectPurchaseOrderCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<PurchaseOrderResponse>>;
