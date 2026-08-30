namespace ZARI.Application.Features.Sales.PosClosing.RunZReading;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>End-of-day BIR close. Runs the same aggregation as X-Reading, but persists a permanent
/// ZReading row and increments Branch.ZCounter by exactly 1. No workflow/ApprovalRequest — this is
/// a direct operational action, not a Draft/Approve document; once run, it is permanent.</summary>
public sealed record RunZReadingCommand(string BranchId, string RunBy) : ICommand<Result<ZReadingResponse>>;

public sealed record ZReadingResponse(
    Guid Id,
    string BranchId,
    int ZCounterValue,
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
    decimal NetSales,
    DateTimeOffset RunAt,
    string? RunBy);
