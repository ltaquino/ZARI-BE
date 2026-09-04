namespace ZARI.Application.Features.Sales.Reports.CashReceiptsBook;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>The BIR Cash Receipts Book: every Customer Payment, sorted chronologically, with a
/// running total that accumulates POSTED payments only (every row is still returned regardless of
/// status). BranchId narrows to one branch; omitted, returns every branch's.</summary>
public sealed record GetCashReceiptsBookReportQuery(string? BranchId) : IQuery<Result<CashReceiptsBookReportResponse>>;

public sealed record CashReceiptsBookReportResponse(List<CashReceiptsBookRow> Rows, decimal TotalReceived);

public sealed record CashReceiptsBookRow(
    Guid PaymentId,
    DateTimeOffset PaymentDate,
    string PaymentNo,
    string CustomerName,
    string Method,
    string BranchId,
    string? RefNo,
    string Status,
    decimal Amount,
    decimal RunningTotal);
