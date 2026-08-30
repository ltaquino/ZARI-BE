namespace ZARI.Application.Features.Sales.PosClosing.RunXReading;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>Read-only snapshot of sales since the last Z-Reading, up to right now. Can be run any
/// number of times a day — never writes anything.</summary>
public sealed record RunXReadingQuery(string BranchId) : IQuery<Result<XReadingResponse>>;

public sealed record XReadingResponse(
    string BranchId,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    int InvoiceCount,
    string? FirstOrNumber,
    string? LastOrNumber,
    decimal GrossSales,
    decimal TotalDiscounts,
    decimal VatableSales,
    decimal VatAmount,
    decimal VatExemptSales,
    decimal ZeroRatedSales,
    decimal NetSales);
