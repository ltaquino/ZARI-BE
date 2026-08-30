namespace ZARI.Application.Features.Sales.SalesOrders.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesOrders.Create;
using ZARI.Application.Features.Sales.SalesOrders.GetAll;
using ZARI.Domain.Common;

public sealed record UpdateSalesOrderCommand(
    Guid Id,
    string BranchId,
    Guid CustomerId,
    DateTimeOffset OrderDate,
    DateTimeOffset? ExpectedDeliveryDate,
    string? Remarks,
    decimal? DiscountPct,
    string? UpdatedBy,
    List<SalesOrderLineInput> Lines) : ICommand<Result<SalesOrderResponse>>;
