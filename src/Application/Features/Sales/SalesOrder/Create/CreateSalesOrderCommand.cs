namespace ZARI.Application.Features.Sales.SalesOrders.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesOrders.GetAll;
using ZARI.Domain.Common;

public sealed record SalesOrderLineInput(
    Guid ItemId,
    decimal Qty,
    Guid UomId,
    decimal UnitPrice,
    decimal DiscountPct,
    string? DiscountSourceType,
    Guid? DiscountSourceId);

public sealed record CreateSalesOrderCommand(
    string BranchId,
    Guid CustomerId,
    DateTimeOffset OrderDate,
    DateTimeOffset? ExpectedDeliveryDate,
    string? Remarks,
    decimal? DiscountPct,
    string? CreatedBy,
    List<SalesOrderLineInput> Lines) : ICommand<Result<SalesOrderResponse>>;
