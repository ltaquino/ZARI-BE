namespace ZARI.Application.Features.Purchasing.PurchaseOrders.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseOrders.Create;
using ZARI.Application.Features.Purchasing.PurchaseOrders.GetAll;
using ZARI.Domain.Common;

public sealed record UpdatePurchaseOrderCommand(
    Guid Id,
    string BranchId,
    Guid SupplierId,
    DateTimeOffset OrderDate,
    DateTimeOffset? ExpectedDate,
    string? Remarks,
    Guid? PurchaseRequestId,
    string? UpdatedBy,
    List<PurchaseOrderLineInput> Lines) : ICommand<Result<PurchaseOrderResponse>>;
