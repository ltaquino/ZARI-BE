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
    Guid? DeliveryOrderLineId,
    string? SerialNo);

public sealed record SalesInvoicePaymentSummary(
    Guid CustomerPaymentId,
    string PaymentNo,
    DateTimeOffset PaymentDate,
    string PaymentMethod,
    decimal AmountApplied);

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
    Guid? PosTerminalId,
    List<SalesInvoiceLineResponse> Lines,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    int PrintCount,
    DateTimeOffset? FirstPrintedAt,
    DateTimeOffset? LastPrintedAt,
    /// <summary>Populated on the single Get-by-id query only (receipt printing needs it); GetAll leaves it empty to avoid an N+1 fan-out on the list view.</summary>
    List<SalesInvoicePaymentSummary> Payments);
