namespace ZARI.Application.Features.Sales.PosTerminals.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetPosTerminalQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetPosTerminalQuery, Result<PosTerminalResponse>>
{
    public async Task<Result<PosTerminalResponse>> HandleAsync(GetPosTerminalQuery query, CancellationToken cancellationToken = default)
    {
        var terminal = await dbContext.PosTerminals.FirstOrDefaultAsync(t => t.Id == query.Id, cancellationToken);
        if (terminal is null)
            return Result.Failure<PosTerminalResponse>(Error.NotFound("PosTerminal.NotFound", $"POS terminal with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("POS_TERMINALS", FormAction.View, terminal.BranchId, cancellationToken))
            return Result.Failure<PosTerminalResponse>(Error.Forbidden("PosTerminal.Forbidden", "You do not have permission to view POS terminals for this branch."));

        var response = new PosTerminalResponse(terminal.Id, terminal.BranchId, terminal.Code, terminal.Name, terminal.MachineIdentificationNumber, terminal.MachineSerialNumber, terminal.PosPermitNumber, terminal.PosPermitDateIssued, terminal.Status, terminal.CreatedAt);
        return Result.Success(response);
    }
}
