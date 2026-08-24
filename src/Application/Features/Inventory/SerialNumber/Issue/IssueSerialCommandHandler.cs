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

        if (existing is null || existing.Status != "IN_STOCK")
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
