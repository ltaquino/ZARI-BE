namespace ZARI.Application.Features.Purchasing.PurchaseOrders.RequestCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseOrders.GetAll;
using ZARI.Domain.Common;

public sealed record RequestPurchaseOrderCancellationCommand(Guid Id, string RequestedBy, string Reason) : ICommand<Result<PurchaseOrderResponse>>;
