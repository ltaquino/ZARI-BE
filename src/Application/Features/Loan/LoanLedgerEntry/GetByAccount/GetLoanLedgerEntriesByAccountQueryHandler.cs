namespace ZARI.Application.Features.Loan.LoanLedgerEntries.GetByAccount;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>Read-only view of a LoanAccount's ledger — no separate Form/permission code; gated by the same LOAN_ACCOUNTS view permission as the account itself, since the ledger is purely a sub-view of it.</summary>
public sealed class GetLoanLedgerEntriesByAccountQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetLoanLedgerEntriesByAccountQuery, Result<List<LoanLedgerEntryResponse>>>
{
    public async Task<Result<List<LoanLedgerEntryResponse>>> HandleAsync(GetLoanLedgerEntriesByAccountQuery query, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.LoanAccounts.FirstOrDefaultAsync(a => a.Id == query.LoanAccountId, cancellationToken);
        if (account is null)
            return Result.Failure<List<LoanLedgerEntryResponse>>(Error.NotFound("LoanAccount.NotFound", $"Loan account with ID '{query.LoanAccountId}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_ACCOUNTS", FormAction.View, account.BranchId, cancellationToken))
            return Result.Failure<List<LoanLedgerEntryResponse>>(Error.Forbidden("LoanAccount.Forbidden", "You do not have permission to view loan accounts for this branch."));

        var entries = await dbContext.LoanLedgerEntries.AsNoTracking()
            .Where(l => l.LoanAccountId == query.LoanAccountId)
            .OrderBy(l => l.SequenceNo)
            .Select(l => new LoanLedgerEntryResponse(
                l.Id, l.SequenceNo, l.EntryDate, l.TransactionType, l.ReferenceTable, l.ReferenceId,
                l.PrincipalIn, l.PrincipalOut, l.RunningPrincipalBalance, l.IsReversal, l.Description, l.PostedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(entries);
    }
}
