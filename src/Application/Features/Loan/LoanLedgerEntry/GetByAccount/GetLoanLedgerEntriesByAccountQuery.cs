namespace ZARI.Application.Features.Loan.LoanLedgerEntries.GetByAccount;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetLoanLedgerEntriesByAccountQuery(Guid LoanAccountId) : IQuery<Result<List<LoanLedgerEntryResponse>>>;

public sealed record LoanLedgerEntryResponse(
    Guid Id,
    int SequenceNo,
    DateTimeOffset EntryDate,
    string TransactionType,
    string ReferenceTable,
    string ReferenceId,
    decimal PrincipalIn,
    decimal PrincipalOut,
    decimal RunningPrincipalBalance,
    bool IsReversal,
    string? Description,
    DateTimeOffset PostedAt);
