namespace ZARI.Application.Features.Sales.PosTerminals.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdatePosTerminalCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<UpdatePosTerminalCommand>
{
    public async Task<Result> HandleAsync(UpdatePosTerminalCommand command, CancellationToken cancellationToken = default)
    {
        var terminal = await dbContext.PosTerminals.FindAsync([command.Id], cancellationToken);
        if (terminal is null)
            return Result.Failure(Error.NotFound("PosTerminal.NotFound", $"POS terminal with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("POS_TERMINALS", FormAction.Edit, terminal.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("PosTerminal.Forbidden", "You do not have permission to update POS terminals for this branch."));

        var duplicateCode = await dbContext.PosTerminals
            .AnyAsync(t => t.Id != command.Id && t.BranchId == terminal.BranchId && t.Code == command.Code, cancellationToken);
        if (duplicateCode)
            return Result.Failure(Error.Conflict("PosTerminal.DuplicateCode", $"A POS terminal with code '{command.Code}' already exists at this branch."));

        terminal.Code = command.Code;
        terminal.Name = command.Name;
        terminal.MachineIdentificationNumber = command.MachineIdentificationNumber;
        terminal.MachineSerialNumber = command.MachineSerialNumber;
        terminal.PosPermitNumber = command.PosPermitNumber;
        terminal.PosPermitDateIssued = command.PosPermitDateIssued;
        terminal.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
