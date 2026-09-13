namespace ZARI.Application.Features.Loan.LoanLedgerEntries.Post;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record PostLoanLedgerEntryResponse(Guid Id, decimal RunningPrincipalBalance);

public sealed record PostLoanLedgerEntryCommand(
    Guid LoanAccountId,
    DateTimeOffset EntryDate,
    string TransactionType,
    string ReferenceTable,
    string ReferenceId,
    decimal PrincipalIn,
    decimal PrincipalOut,
    string? Description,
    bool IsReversal) : ICommand<Result<PostLoanLedgerEntryResponse>>;
