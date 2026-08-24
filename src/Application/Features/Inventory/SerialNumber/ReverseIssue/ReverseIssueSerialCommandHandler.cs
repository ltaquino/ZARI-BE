namespace ZARI.Application.Features.Inventory.SerialNumbers.ReverseIssue;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class ReverseIssueSerialCommandHandler(IAppDbContext dbContext) : ICommandHandler<ReverseIssueSerialCommand, Result>
{
    public async Task<Result> HandleAsync(ReverseIssueSerialCommand command, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.SerialNumbers
            .FirstOrDefaultAsync(s => s.ItemId == command.ItemId && s.SerialNo == command.SerialNo, cancellationToken);

        if (existing is null || existing.Status == "IN_STOCK")
            return Result.Success();

        existing.Status = "IN_STOCK";
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
