namespace ZARI.Application.Features.Hr.Positions.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.Positions.Get;
using ZARI.Domain.Common;

public sealed class GetAllPositionsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllPositionsQuery, Result<List<PositionResponse>>>
{
    public async Task<Result<List<PositionResponse>>> HandleAsync(GetAllPositionsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("POSITIONS", FormAction.View, cancellationToken))
            return Result.Failure<List<PositionResponse>>(Error.Forbidden("Position.Forbidden", "You do not have permission to view positions."));

        var items = await dbContext.Positions.AsNoTracking()
            .OrderBy(p => p.Code)
            .Select(p => new PositionResponse(p.Id, p.Code, p.Name, p.DepartmentId, p.Status, p.PayGradeMin, p.PayGradeMax, p.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
