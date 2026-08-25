namespace ZARI.Application.Features.Accounting.GlJournals.Reverse;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Reverses the journal(s) originally posted for (sourceReferenceTable, sourceReferenceId) by
/// posting a new journal per original with every line's debit/credit swapped, and flags each
/// original REVERSED — mirrors the FE prototype's reverseJournalsFor. A no-op (not an error) if
/// nothing was ever posted for this reference, since some cancelled documents never reach the
/// point of posting a journal in the first place.
/// </summary>
public sealed class ReverseGlJournalsCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler)
    : ICommandHandler<ReverseGlJournalsCommand, Result<List<GlJournalResponse>>>
{
    public async Task<Result<List<GlJournalResponse>>> HandleAsync(ReverseGlJournalsCommand command, CancellationToken cancellationToken = default)
    {
        var originals = await dbContext.GlJournals
            .Include(j => j.Lines)
            .Where(j => j.SourceReferenceTable == command.SourceReferenceTable && j.SourceReferenceId == command.SourceReferenceId && j.Status == "POSTED")
            .ToListAsync(cancellationToken);

        if (originals.Count == 0)
            return Result.Success(new List<GlJournalResponse>());

        var reversals = new List<GlJournal>();
        foreach (var original in originals)
        {
            var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(original.BranchId, "JV"), cancellationToken);
            if (!numberResult.IsSuccess)
                return Result.Failure<List<GlJournalResponse>>(numberResult.Error!);

            var reversal = new GlJournal
            {
                JournalNo = numberResult.Value!.DocumentNumber,
                BranchId = original.BranchId,
                JournalDate = command.JournalDate,
                SourceModule = original.SourceModule,
                SourceReferenceTable = command.SourceReferenceTable,
                SourceReferenceId = command.SourceReferenceId,
                Description = command.Description ?? $"Reversal of {original.JournalNo}",
                Status = "POSTED",
                ReversalOfJournalId = original.Id,
                Lines = original.Lines.Select(l => new GlJournalLine
                {
                    AccountId = l.AccountId,
                    CostCenterId = l.CostCenterId,
                    DebitAmount = l.CreditAmount,
                    CreditAmount = l.DebitAmount,
                    Memo = l.Memo
                }).ToList()
            };

            original.Status = "REVERSED";
            dbContext.GlJournals.Add(reversal);
            reversals.Add(reversal);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(reversals.Select(GlJournalMapper.ToResponse).ToList());
    }
}
