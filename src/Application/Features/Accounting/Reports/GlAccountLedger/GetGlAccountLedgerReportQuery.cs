namespace ZARI.Application.Features.Accounting.Reports.GlAccountLedger;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetGlAccountLedgerReportQuery(Guid AccountId, string? BranchId, DateTimeOffset? FromDate, DateTimeOffset? ToDate)
    : IQuery<Result<GlAccountLedgerReportResponse>>;

public sealed record GlAccountLedgerLine(Guid JournalId, string JournalNo, DateTimeOffset JournalDate, string BranchId, string? Memo, decimal Debit, decimal Credit, decimal RunningBalance);

public sealed record GlAccountLedgerReportResponse(
    string AccountCode,
    string AccountName,
    decimal Opening,
    decimal Closing,
    decimal PeriodDebit,
    decimal PeriodCredit,
    List<GlAccountLedgerLine> Lines);
