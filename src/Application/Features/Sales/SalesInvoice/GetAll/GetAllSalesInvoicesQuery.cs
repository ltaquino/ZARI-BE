namespace ZARI.Application.Features.Sales.SalesInvoices.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllSalesInvoicesQuery : IQuery<Result<List<SalesInvoiceResponse>>>;

public sealed record SalesInvoiceLineResponse(
    Guid Id,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    string? ItemDescription,
    decimal Qty,
    Guid UomId,
    string UomCode,
    decimal UnitPrice,
    decimal DiscountPct,
    string? DiscountSourceType,
    Guid? DiscountSourceId,
    string VatType,
    Guid? StatutoryDiscountTypeId,
    string? StatutoryDiscountTypeName,
    string? StatutoryIdNumber,
    Guid? DeliveryOrderLineId);

public sealed record SalesInvoiceResponse(
    Guid Id,
    string InvoiceNo,
    string BranchId,
    Guid CustomerId,
    string CustomerName,
    Guid? DeliveryOrderId,
    DateTimeOffset InvoiceDate,
    DateTimeOffset? DueDate,
    string Status,
    string? Remarks,
    decimal? DiscountPct,
    string? BirOrSeriesNumber,
    decimal PaidAmount,
    /// <summary>
    /// Live-computed from POSTED CustomerPayment lines (SalesInvoicePaymentBalance), NOT the same
    /// as the PaidAmount stored field above (which Wave 3 left permanently 0 and is never written).
    /// </summary>
    decimal AmountPaid,
    decimal Balance,
    Guid? CostCenterId,
    List<SalesInvoiceLineResponse> Lines,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
