namespace ZARI.Application.Features.Accounting.GlJournals.Post;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Posts a balanced GL journal for an inventory transaction — mirrors the FE prototype's
/// postJournal (data/accounting/glJournals.ts). Reuses the already-migrated document-numbering
/// handler for the journal number rather than duplicating its atomic compare-and-swap logic.
/// </summary>
public sealed class PostGlJournalCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler)
    : ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>>
{
    public async Task<Result<GlJournalResponse>> HandleAsync(PostGlJournalCommand command, CancellationToken cancellationToken = default)
    {
        // Every line must move money in exactly one direction — both sides populated (or both
        // zero) would still balance at the total level but is never a real accounting entry. This
        // is the authoritative check for every internal caller now (PostGlJournalValidator only
        // ever ran against the raw HTTP endpoint, which has been removed — see GlJournalEndpoints).
        var malformedLine = command.Lines.FirstOrDefault(l => (l.DebitAmount > 0) == (l.CreditAmount > 0));
        if (malformedLine is not null)
        {
            return Result.Failure<GlJournalResponse>(Error.Validation(
                "GlJournal.MalformedLine", "Every journal line must have exactly one of DebitAmount or CreditAmount greater than zero."));
        }

        var totalDebit = Math.Round(command.Lines.Sum(l => l.DebitAmount), 4);
        var totalCredit = Math.Round(command.Lines.Sum(l => l.CreditAmount), 4);
        if (totalDebit != totalCredit)
        {
            return Result.Failure<GlJournalResponse>(Error.Validation(
                "GlJournal.Unbalanced", $"Journal is unbalanced: debit {totalDebit} vs credit {totalCredit}."));
        }

        var fiscalYear = await dbContext.FiscalYears.FirstOrDefaultAsync(
            fy => command.JournalDate >= fy.StartDate && command.JournalDate <= fy.EndDate, cancellationToken);
        if (fiscalYear is not null && fiscalYear.Status == "CLOSED")
        {
            return Result.Failure<GlJournalResponse>(Error.Validation(
                "GlJournal.PeriodClosed",
                $"Cannot post on {command.JournalDate:yyyy-MM-dd} — {fiscalYear.YearName} is closed."));
        }

        var accountIds = command.Lines.Select(l => l.AccountId).Distinct().ToList();
        var existingAccountCount = await dbContext.GlAccounts.CountAsync(a => accountIds.Contains(a.Id), cancellationToken);
        if (existingAccountCount != accountIds.Count)
        {
            return Result.Failure<GlJournalResponse>(Error.NotFound(
                "GlAccount.NotFound", "One or more GL accounts referenced by this journal were not found."));
        }

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "JV"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<GlJournalResponse>(numberResult.Error!);

        var journal = new GlJournal
        {
            JournalNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            JournalDate = command.JournalDate,
            SourceModule = command.SourceModule,
            SourceReferenceTable = command.SourceReferenceTable,
            SourceReferenceId = command.SourceReferenceId,
            Description = command.Description,
            Status = "POSTED",
            Lines = command.Lines.Select(l => new GlJournalLine
            {
                AccountId = l.AccountId,
                CostCenterId = l.CostCenterId,
                DebitAmount = l.DebitAmount,
                CreditAmount = l.CreditAmount,
                Memo = l.Memo
            }).ToList()
        };

        dbContext.GlJournals.Add(journal);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(GlJournalMapper.ToResponse(journal));
    }
}
