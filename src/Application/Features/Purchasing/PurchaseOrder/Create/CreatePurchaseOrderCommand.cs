namespace ZARI.Application.Features.Purchasing.PurchaseOrders.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseOrders.GetAll;
using ZARI.Domain.Common;

public sealed record PurchaseOrderLineInput(Guid ItemId, decimal Qty, Guid UomId, decimal UnitCost);

public sealed record CreatePurchaseOrderCommand(
    string BranchId,
    Guid SupplierId,
    DateTimeOffset OrderDate,
    DateTimeOffset? ExpectedDate,
    string? Remarks,
    string? CreatedBy,
    List<PurchaseOrderLineInput> Lines) : ICommand<Result<PurchaseOrderResponse>>;
