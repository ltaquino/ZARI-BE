namespace ZARI.Application.Features.Hr.Positions.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetPositionQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetPositionQuery, Result<PositionResponse>>
{
    public async Task<Result<PositionResponse>> HandleAsync(GetPositionQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("POSITIONS", FormAction.View, cancellationToken))
            return Result.Failure<PositionResponse>(Error.Forbidden("Position.Forbidden", "You do not have permission to view positions."));

        var position = await dbContext.Positions
            .Where(p => p.Id == query.Id)
            .Select(p => new PositionResponse(p.Id, p.Code, p.Name, p.DepartmentId, p.Status, p.PayGradeMin, p.PayGradeMax, p.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (position is null)
            return Result.Failure<PositionResponse>(Error.NotFound("Position.NotFound", $"Position with ID '{query.Id}' was not found."));

        return Result.Success(position);
    }
}
