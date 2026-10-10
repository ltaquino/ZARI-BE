namespace ZARI.Application.Features.Sales.PosTerminals.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.PosTerminals.Get;
using ZARI.Application.Features.Sales.PosTerminals.Shared;
using ZARI.Domain.Common;

public sealed class GetAllPosTerminalsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllPosTerminalsQuery, Result<List<PosTerminalResponse>>>
{
    public async Task<Result<List<PosTerminalResponse>>> HandleAsync(GetAllPosTerminalsQuery query, CancellationToken cancellationToken = default)
    {
        var candidates = await dbContext.PosTerminals.AsNoTracking()
            .Where(t => query.BranchId == null || t.BranchId == query.BranchId)
            .OrderBy(t => t.BranchId).ThenBy(t => t.Code)
            .ToListAsync(cancellationToken);

        var results = new List<PosTerminalResponse>();
        foreach (var terminal in candidates)
        {
            if (!await permissionService.HasPermissionOnBranchAsync("POS_TERMINALS", FormAction.View, terminal.BranchId, cancellationToken))
                continue;

            results.Add(PosTerminalMapper.ToResponse(terminal));
        }

        return Result.Success(results);
    }
}
