namespace ZARI.Application.Features.Inventory.Uoms.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Uoms.Get;
using ZARI.Domain.Common;

public sealed class GetAllUomsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllUomsQuery, Result<List<UomResponse>>>
{
    public async Task<Result<List<UomResponse>>> HandleAsync(GetAllUomsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("UOMS", FormAction.View, cancellationToken))
            return Result.Failure<List<UomResponse>>(Error.Forbidden("Uom.Forbidden", "You do not have permission to view UOMs."));

        var items = await dbContext.Uoms
            .OrderBy(u => u.Code)
            .Select(u => new UomResponse(u.Id, u.Code, u.Name, u.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
