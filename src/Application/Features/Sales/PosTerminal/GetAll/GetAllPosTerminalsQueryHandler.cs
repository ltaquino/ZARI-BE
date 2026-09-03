namespace ZARI.Application.Features.Sales.PosTerminals.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.PosTerminals.Get;
using ZARI.Domain.Common;

public sealed class GetAllPosTerminalsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllPosTerminalsQuery, Result<List<PosTerminalResponse>>>
{
    public async Task<Result<List<PosTerminalResponse>>> HandleAsync(GetAllPosTerminalsQuery query, CancellationToken cancellationToken = default)
    {
        var candidates = await dbContext.PosTerminals
            .Where(t => query.BranchId == null || t.BranchId == query.BranchId)
            .OrderBy(t => t.BranchId).ThenBy(t => t.Code)
            .ToListAsync(cancellationToken);

        var results = new List<PosTerminalResponse>();
        foreach (var terminal in candidates)
        {
            if (!await permissionService.HasPermissionOnBranchAsync("POS_TERMINALS", FormAction.View, terminal.BranchId, cancellationToken))
                continue;

            results.Add(new PosTerminalResponse(terminal.Id, terminal.BranchId, terminal.Code, terminal.Name, terminal.MachineIdentificationNumber, terminal.MachineSerialNumber, terminal.PosPermitNumber, terminal.PosPermitDateIssued, terminal.Status, terminal.CreatedAt));
        }

        return Result.Success(results);
    }
}
