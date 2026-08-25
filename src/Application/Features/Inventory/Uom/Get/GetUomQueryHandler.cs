namespace ZARI.Application.Features.Inventory.Uoms.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetUomQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetUomQuery, Result<UomResponse>>
{
    public async Task<Result<UomResponse>> HandleAsync(GetUomQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("UOMS", FormAction.View, cancellationToken))
            return Result.Failure<UomResponse>(Error.Forbidden("Uom.Forbidden", "You do not have permission to view UOMs."));

        var uom = await dbContext.Uoms
            .Where(u => u.Id == query.Id)
            .Select(u => new UomResponse(u.Id, u.Code, u.Name, u.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (uom is null)
            return Result.Failure<UomResponse>(Error.NotFound("Uom.NotFound", $"UOM with ID '{query.Id}' was not found."));

        return Result.Success(uom);
    }
}
