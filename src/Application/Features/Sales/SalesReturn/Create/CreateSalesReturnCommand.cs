namespace ZARI.Application.Features.Sales.SalesReturns.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesReturns.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// VatType is a manual, input-only fallback used purely for the quick-post GL computation when
/// DeliveryOrderLineId is not set — SalesReturnLine has no column to persist it, so supply it again
/// on every call that needs it (see SalesReturnPostingService's own doc comment). Ignored entirely
/// when DeliveryOrderLineId is set, since the original sale's own VAT treatment is looked up instead.
/// </summary>
public sealed record SalesReturnLineInput(
    Guid ItemId,
    decimal QtyReturned,
    Guid UomId,
    decimal UnitPrice,
    Guid? DeliveryOrderLineId,
    string? VatType);

public sealed record CreateSalesReturnCommand(
    string BranchId,
    Guid WarehouseId,
    Guid CustomerId,
    Guid? DeliveryOrderId,
    DateTimeOffset ReturnDate,
    string? Remarks,
    Guid? CostCenterId,
    string? CreatedBy,
    List<SalesReturnLineInput> Lines) : ICommand<Result<SalesReturnResponse>>;
