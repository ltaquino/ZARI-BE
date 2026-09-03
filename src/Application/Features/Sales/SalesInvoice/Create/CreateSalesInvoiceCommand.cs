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
    Guid? DeliveryOrderLineId,
    // Which physical unit this line sells, for a serialized item — only ever meaningful (and only
    // ever enforced as required) on a POS checkout; see SalesInvoiceLine.SerialNo's own doc comment.
    string? SerialNo = null);

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
    List<SalesInvoiceLineInput> Lines,
    // POS Mode's own checkout call (CreatePosSaleCommand) sets these — everyone else leaves them
    // at their defaults. ForceQuickPost attempts immediate posting regardless of
    // Company.SalesInvoiceQuickPostEnabled (a checkout counter can't wait for a setting toggle),
    // but the MaxUnapprovedDiscountPct threshold check below still applies unchanged either way.
    bool ForceQuickPost = false,
    Guid? PosTerminalId = null) : ICommand<Result<SalesInvoiceResponse>>;
