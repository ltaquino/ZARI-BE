namespace ZARI.Application.Features.Purchasing.Reports.PurchaseBook;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>
/// The BIR Purchase Book — one row per POSTED AP invoice, VATable/Zero-Rated/Exempt/Input-Tax
/// columns computed from each line's VatType classification. Doesn't reflect any GL posting change —
/// ApInvoice still posts UnitCost*Qty/Amount exactly as before VatType existed; this report is
/// purely a reporting-layer view of the same figures.
/// </summary>
public sealed record GetPurchaseBookReportQuery(string? BranchId, Guid? SupplierId) : IQuery<Result<PurchaseBookReportResponse>>;

public sealed record PurchaseBookRow(
    Guid InvoiceId,
    DateTimeOffset InvoiceDate,
    string SupplierName,
    string? SupplierTaxId,
    string SupplierInvoiceNo,
    string BranchId,
    decimal Gross,
    decimal VatableSales,
    decimal ZeroRated,
    decimal Exempt,
    decimal InputTax);

public sealed record PurchaseBookReportResponse(
    List<PurchaseBookRow> Rows,
    decimal TotalGross,
    decimal TotalVatableSales,
    decimal TotalZeroRated,
    decimal TotalExempt,
    decimal TotalInputTax);
