namespace ZARI.Application.Features.Sales.SalesInvoices.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesInvoices.Create;
using ZARI.Application.Features.Sales.SalesInvoices.GetAll;
using ZARI.Domain.Common;

public sealed record UpdateSalesInvoiceCommand(
    Guid Id,
    string BranchId,
    Guid CustomerId,
    Guid? DeliveryOrderId,
    DateTimeOffset InvoiceDate,
    DateTimeOffset? DueDate,
    string? Remarks,
    decimal? DiscountPct,
    Guid? CostCenterId,
    string? UpdatedBy,
    List<SalesInvoiceLineInput> Lines) : ICommand<Result<SalesInvoiceResponse>>;
