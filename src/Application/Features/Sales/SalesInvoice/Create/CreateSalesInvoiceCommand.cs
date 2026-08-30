namespace ZARI.Application.Features.Sales.SalesInvoices.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesInvoices.GetAll;
using ZARI.Domain.Common;

public sealed record SalesInvoiceLineInput(
    Guid ItemId,
    decimal Qty,
    Guid UomId,
    decimal UnitPrice,
    decimal DiscountPct,
    string? DiscountSourceType,
    Guid? DiscountSourceId,
    string VatType,
    Guid? StatutoryDiscountTypeId,
    string? StatutoryIdNumber,
    Guid? DeliveryOrderLineId);

public sealed record CreateSalesInvoiceCommand(
    string BranchId,
    Guid CustomerId,
    Guid? DeliveryOrderId,
    DateTimeOffset InvoiceDate,
    DateTimeOffset? DueDate,
    string? Remarks,
    decimal? DiscountPct,
    Guid? CostCenterId,
    string? CreatedBy,
    List<SalesInvoiceLineInput> Lines) : ICommand<Result<SalesInvoiceResponse>>;
