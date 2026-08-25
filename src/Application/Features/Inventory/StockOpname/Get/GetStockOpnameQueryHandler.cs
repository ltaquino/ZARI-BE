namespace ZARI.Application.Features.Inventory.StockOpnames.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockOpnames.GetAll;
using ZARI.Application.Features.Inventory.StockOpnames.Shared;
using ZARI.Domain.Common;

public sealed class GetStockOpnameQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetStockOpnameQuery, Result<StockOpnameResponse>>
{
    public async Task<Result<StockOpnameResponse>> HandleAsync(GetStockOpnameQuery query, CancellationToken cancellationToken = default)
    {
        var opname = await dbContext.StockOpnames
            .Include(o => o.Lines).ThenInclude(l => l.Item).ThenInclude(i => i.BaseUom)
            .FirstOrDefaultAsync(o => o.Id == query.Id, cancellationToken);

        if (opname is null)
            return Result.Failure<StockOpnameResponse>(Error.NotFound("StockOpname.NotFound", $"Stock opname with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("STOCK_OPNAMES", FormAction.View, opname.BranchId, cancellationToken))
            return Result.Failure<StockOpnameResponse>(Error.Forbidden("StockOpname.Forbidden", "You do not have permission to view stock opnames for this branch."));

        return Result.Success(StockOpnameMapper.ToResponse(opname));
    }
}
