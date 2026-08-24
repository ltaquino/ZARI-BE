namespace ZARI.Application.Features.Inventory.SerialNumbers.ReverseReceive;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class ReverseReceiveSerialCommandHandler(IAppDbContext dbContext) : ICommandHandler<ReverseReceiveSerialCommand, Result>
{
    public async Task<Result> HandleAsync(ReverseReceiveSerialCommand command, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.SerialNumbers
            .FirstOrDefaultAsync(s => s.ItemId == command.ItemId && s.SerialNo == command.SerialNo, cancellationToken);

        if (existing is null)
            return Result.Success();

        if (command.RevertTo == "REMOVE")
        {
            dbContext.SerialNumbers.Remove(existing);
        }
        else
        {
            existing.Status = "IN_TRANSIT";
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
