namespace ZARI.Application.Features.Loan.LoanLedgerEntries.Post;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Shared append-only ledger-write engine, called by LoanDisbursement's Approve/ApproveCancellation
/// and, later, LoanPayment/LoanRestructuring/LoanWriteOff — never called directly from an endpoint.
/// No caller-facing permission check here (same as ReceiveStockCommandHandler): the calling handler
/// already checked permission for the action that triggers this write.
/// </summary>
public sealed class PostLoanLedgerEntryCommandHandler(IAppDbContext dbContext) : ICommandHandler<PostLoanLedgerEntryCommand, Result<PostLoanLedgerEntryResponse>>
{
    public async Task<Result<PostLoanLedgerEntryResponse>> HandleAsync(PostLoanLedgerEntryCommand command, CancellationToken cancellationToken = default)
    {
        var accountExists = await dbContext.LoanAccounts.AnyAsync(a => a.Id == command.LoanAccountId, cancellationToken);
        if (!accountExists)
            return Result.Failure<PostLoanLedgerEntryResponse>(Error.NotFound("LoanAccount.NotFound", $"Loan account with ID '{command.LoanAccountId}' was not found."));

        // Idempotency guard — a retry of the same reference (in the same direction) must not
        // double-post the same movement. A reversal intentionally shares the reference with the
        // original post, so it's matched separately by IsReversal — "already posted" means a real,
        // still-standing post in that same direction exists, not that one ever did.
        var existing = await dbContext.LoanLedgerEntries
            .Where(l => l.ReferenceTable == command.ReferenceTable && l.ReferenceId == command.ReferenceId && l.IsReversal == command.IsReversal)
            .Select(l => new { l.Id, l.RunningPrincipalBalance })
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
            return Result.Success(new PostLoanLedgerEntryResponse(existing.Id, existing.RunningPrincipalBalance));

        Guid insertedId = default;
        decimal newBalance = 0;

        // The Aspire-configured MySqlRetryingExecutionStrategy refuses to run inside a
        // user-managed BeginTransactionAsync unless the whole unit of work — begin, mutate,
        // commit — is itself the thing being retried.
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            // Locks the single LoanAccount row this ledger is scoped to — a loan account's own
            // ledger has no cross-account contention like stock's item+warehouse+batch keying, so
            // locking the account itself (rather than a separate balance-cache table) is enough to
            // make "read the last row's running balance, then append the next one" race-free.
            await dbContext.LoanAccounts
                .FromSqlInterpolated($"SELECT * FROM LoanAccounts WHERE Id = {command.LoanAccountId} FOR UPDATE")
                .ToListAsync(cancellationToken);

            var last = await dbContext.LoanLedgerEntries
                .Where(l => l.LoanAccountId == command.LoanAccountId)
                .OrderByDescending(l => l.SequenceNo)
                .Select(l => new { l.SequenceNo, l.RunningPrincipalBalance })
                .FirstOrDefaultAsync(cancellationToken);

            var nextSequenceNo = (last?.SequenceNo ?? 0) + 1;
            newBalance = (last?.RunningPrincipalBalance ?? 0) + command.PrincipalIn - command.PrincipalOut;

            var entry = new LoanLedgerEntry
            {
                LoanAccountId = command.LoanAccountId,
                SequenceNo = nextSequenceNo,
                EntryDate = command.EntryDate,
                TransactionType = command.TransactionType,
                ReferenceTable = command.ReferenceTable,
                ReferenceId = command.ReferenceId,
                PrincipalIn = command.PrincipalIn,
                PrincipalOut = command.PrincipalOut,
                RunningPrincipalBalance = newBalance,
                IsReversal = command.IsReversal,
                Description = command.Description,
                PostedAt = DateTimeOffset.UtcNow
            };
            dbContext.LoanLedgerEntries.Add(entry);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            insertedId = entry.Id;
        });

        return Result.Success(new PostLoanLedgerEntryResponse(insertedId, newBalance));
    }
}
