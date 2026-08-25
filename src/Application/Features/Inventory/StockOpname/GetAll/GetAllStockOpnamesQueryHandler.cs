namespace ZARI.Application.Features.Inventory.StockOpnames.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockOpnames.Shared;
using ZARI.Domain.Common;

public sealed class GetAllStockOpnamesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllStockOpnamesQuery, Result<List<StockOpnameResponse>>>
{
    public async Task<Result<List<StockOpnameResponse>>> HandleAsync(GetAllStockOpnamesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("STOCK_OPNAMES", FormAction.View, cancellationToken))
            return Result.Failure<List<StockOpnameResponse>>(Error.Forbidden("StockOpname.Forbidden", "You do not have permission to view stock opnames."));

        var opnames = await dbContext.StockOpnames
            .Include(o => o.Lines).ThenInclude(l => l.Item).ThenInclude(i => i.BaseUom)
            .OrderByDescending(o => o.CountDate)
            .ToListAsync(cancellationToken);

        return Result.Success(opnames.Select(StockOpnameMapper.ToResponse).ToList());
    }
}
