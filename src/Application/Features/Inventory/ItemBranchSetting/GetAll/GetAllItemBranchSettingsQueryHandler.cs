namespace ZARI.Application.Features.Inventory.ItemBranchSettings.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.ItemBranchSettings.Get;
using ZARI.Domain.Common;

public sealed class GetAllItemBranchSettingsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllItemBranchSettingsQuery, Result<List<ItemBranchSettingResponse>>>
{
    public async Task<Result<List<ItemBranchSettingResponse>>> HandleAsync(GetAllItemBranchSettingsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("ITEM_BRANCH_SETTINGS", FormAction.View, cancellationToken))
            return Result.Failure<List<ItemBranchSettingResponse>>(Error.Forbidden("ItemBranchSetting.Forbidden", "You do not have permission to view item branch settings."));

        var items = await dbContext.ItemBranchSettings
            .OrderBy(s => s.BranchId).ThenBy(s => s.ItemId)
            .Select(s => new ItemBranchSettingResponse(s.Id, s.ItemId, s.BranchId, s.DefaultWarehouseId, s.ReorderPoint, s.MinStock, s.MaxStock,
                s.SellingPrice, s.MarkupPct, s.Status, s.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
