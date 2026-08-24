namespace ZARI.Application.Features.System.DocumentSequences.GetNext;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>
/// Reserves and formats the next document number for (BranchId, DocType) atomically. The FE's
/// previous implementation (data/system-module/documentSequences.ts) read NextNumber then wrote
/// it back in a separate call — under concurrent requests, two callers could read the same value
/// and both increment from it, handing out a duplicate document number. This uses a
/// compare-and-swap UPDATE instead: the WHERE clause pins the row to the exact NextNumber value
/// just read, so if another request already claimed it, zero rows match and this retries with a
/// fresh read rather than silently overwriting. No explicit transaction/row-lock needed — the
/// UPDATE itself is atomic at the row level.
/// </summary>
public sealed class GetNextDocumentNumberCommandHandler(IAppDbContext dbContext) : ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>>
{
    private const int MaxAttempts = 20;

    public async Task<Result<NextDocumentNumberResponse>> HandleAsync(GetNextDocumentNumberCommand command, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            if (attempt > 0)
            {
                // A loser retries immediately after losing the compare-and-swap; under a burst of
                // simultaneous callers that just re-collides on the next row version. A little
                // jitter spreads retries out so they stop lining up on the same instant.
                await Task.Delay(Random.Shared.Next(5, 5 + attempt * 10), cancellationToken);
            }

            var sequence = await dbContext.DocumentSequences
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.BranchId == command.BranchId && s.DocType == command.DocType, cancellationToken);

            if (sequence is null)
            {
                // No sequence configured for this branch/doc type — fall back to a timestamp-based
                // number rather than failing the caller's transaction outright.
                var fallback = $"{command.DocType}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                return Result.Success(new NextDocumentNumberResponse(fallback));
            }

            var formatted = $"{sequence.Prefix}{sequence.NextNumber.ToString().PadLeft(sequence.PaddingLength, '0')}";

            var rowsAffected = await dbContext.DocumentSequences
                .Where(s => s.Id == sequence.Id && s.NextNumber == sequence.NextNumber)
                .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.NextNumber, s => s.NextNumber + 1), cancellationToken);

            if (rowsAffected == 1)
                return Result.Success(new NextDocumentNumberResponse(formatted));

            // Another request incremented it between our read and our update — retry with a fresh read.
        }

        return Result.Failure<NextDocumentNumberResponse>(
            Error.Failure("DocumentSequence.Contention", "Could not allocate a document number due to heavy concurrent activity — please try again."));
    }
}
