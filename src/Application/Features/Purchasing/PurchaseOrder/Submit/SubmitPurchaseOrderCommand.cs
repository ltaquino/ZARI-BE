namespace ZARI.Application.Features.Purchasing.PurchaseOrders.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseOrders.GetAll;
using ZARI.Domain.Common;

public sealed record SubmitPurchaseOrderCommand(Guid Id, string RequestedBy) : ICommand<Result<PurchaseOrderResponse>>;
