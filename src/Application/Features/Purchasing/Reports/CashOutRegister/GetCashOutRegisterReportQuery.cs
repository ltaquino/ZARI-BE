namespace ZARI.Application.Features.Purchasing.Reports.CashOutRegister;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>
/// Chronological log of outgoing payments — a running total of cash/bank funds actually paid out to
/// suppliers. Doubles as the BIR Cash Disbursements Book.
/// </summary>
public sealed record GetCashOutRegisterReportQuery(string? BranchId, Guid? BankAccountId) : IQuery<Result<CashOutRegisterReportResponse>>;

public sealed record CashOutRegisterRow(
    Guid PaymentId,
    DateTimeOffset PaymentDate,
    string PaymentNo,
    string SupplierName,
    string BankAccountName,
    string BranchId,
    string? RefNo,
    string Status,
    decimal Amount,
    decimal RunningTotal);

public sealed record CashOutRegisterReportResponse(List<CashOutRegisterRow> Rows, decimal TotalPaidOut);
