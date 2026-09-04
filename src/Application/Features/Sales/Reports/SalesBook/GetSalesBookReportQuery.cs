namespace ZARI.Application.Features.Sales.Reports.SalesBook;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>The BIR Sales Book: every non-DRAFT, non-CANCELLED Sales Invoice, VAT-split into
/// VATABLE/ZERO_RATED/VAT_EXEMPT. BranchId narrows to one branch; omitted, returns every branch's.</summary>
public sealed record GetSalesBookReportQuery(string? BranchId) : IQuery<Result<SalesBookReportResponse>>;

public sealed record SalesBookReportResponse(
    List<SalesBookRow> Rows,
    decimal TotalGross,
    decimal TotalVatableSales,
    decimal TotalZeroRated,
    decimal TotalExempt,
    decimal TotalVatAmount);

public sealed record SalesBookRow(
    Guid InvoiceId,
    DateTimeOffset InvoiceDate,
    string CustomerName,
    string? BirOrSeriesNumber,
    string InvoiceNo,
    string BranchId,
    decimal Gross,
    decimal VatableSales,
    decimal ZeroRated,
    decimal Exempt,
    decimal VatAmount);
