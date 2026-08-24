namespace ZARI.Application.Features.Inventory.StockOpnames.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteStockOpnameCommandHandler(IAppDbContext dbContext) : ICommandHandler<DeleteStockOpnameCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteStockOpnameCommand command, CancellationToken cancellationToken = default)
    {
        var opname = await dbContext.StockOpnames.FindAsync([command.Id], cancellationToken);
        if (opname is null)
            return Result.Failure(Error.NotFound("StockOpname.NotFound", $"Stock opname with ID '{command.Id}' was not found."));

        if (opname.Status != "DRAFT")
            return Result.Failure(Error.Validation("StockOpname.NotDraft", "Only a draft stock count can be deleted — cancel it instead."));

        dbContext.StockOpnames.Remove(opname);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
