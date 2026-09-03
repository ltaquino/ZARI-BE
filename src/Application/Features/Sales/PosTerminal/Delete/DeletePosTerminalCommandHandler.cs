namespace ZARI.Application.Features.Sales.PosTerminals.Delete;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeletePosTerminalCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeletePosTerminalCommand>
{
    public async Task<Result> HandleAsync(DeletePosTerminalCommand command, CancellationToken cancellationToken = default)
    {
        var terminal = await dbContext.PosTerminals.FindAsync([command.Id], cancellationToken);
        if (terminal is null)
            return Result.Failure(Error.NotFound("PosTerminal.NotFound", $"POS terminal with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("POS_TERMINALS", FormAction.Delete, terminal.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("PosTerminal.Forbidden", "You do not have permission to delete POS terminals for this branch."));

        var inUse = await dbContext.SalesInvoices.AnyAsync(i => i.PosTerminalId == command.Id, cancellationToken);
        if (inUse)
            return Result.Failure(Error.Conflict("PosTerminal.InUse", "This terminal has been used on at least one sale and cannot be deleted — set it to inactive instead."));

        dbContext.PosTerminals.Remove(terminal);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
