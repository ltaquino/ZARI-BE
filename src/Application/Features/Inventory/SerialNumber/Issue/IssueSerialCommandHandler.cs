namespace ZARI.Application.Features.Inventory.SerialNumbers.Issue;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class IssueSerialCommandHandler(IAppDbContext dbContext) : ICommandHandler<IssueSerialCommand, Result>
{
    public async Task<Result> HandleAsync(IssueSerialCommand command, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.SerialNumbers
            .FirstOrDefaultAsync(s => s.ItemId == command.ItemId && s.SerialNo == command.SerialNo, cancellationToken);

        if (existing is null)
        {
            return Result.Failure(Error.Validation(
                "SerialNumber.NotInStock",
                $"Serial {command.SerialNo} is not currently in stock and cannot be issued."));
        }

        // Idempotency guard — a resumed Approve retry (after a later posting step failed on a
        // prior attempt) calls this again for a serial it already issued; unlike stock ledger
        // rows, a serial's own Status has no per-attempt history to check, so "already at the
        // target disposition" is itself the idempotent-no-op signal.
        if (existing.Status == command.Disposition)
            return Result.Success();

        if (existing.Status != "IN_STOCK")
        {
            return Result.Failure(Error.Validation(
                "SerialNumber.NotInStock",
                $"Serial {command.SerialNo} is not currently in stock and cannot be issued."));
        }

        existing.Status = command.Disposition;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
