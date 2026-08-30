namespace ZARI.Application.Features.Sales.SalesReturns.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesReturns.Create;
using ZARI.Application.Features.Sales.SalesReturns.GetAll;
using ZARI.Domain.Common;

public sealed record UpdateSalesReturnCommand(
    Guid Id,
    string BranchId,
    Guid WarehouseId,
    Guid CustomerId,
    Guid? DeliveryOrderId,
    DateTimeOffset ReturnDate,
    string? Remarks,
    Guid? CostCenterId,
    string? UpdatedBy,
    List<SalesReturnLineInput> Lines) : ICommand<Result<SalesReturnResponse>>;
