namespace ZARI.Application.Features.Inventory.ItemBranchSettings.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteItemBranchSettingCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteItemBranchSettingCommand>
{
    public async Task<Result> HandleAsync(DeleteItemBranchSettingCommand command, CancellationToken cancellationToken = default)
    {
        var setting = await dbContext.ItemBranchSettings.FindAsync([command.Id], cancellationToken);
        if (setting is null)
            return Result.Failure(Error.NotFound("ItemBranchSetting.NotFound", $"Reorder setting with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("ITEM_BRANCH_SETTINGS", FormAction.Delete, setting.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("ItemBranchSetting.Forbidden", "You do not have permission to delete item branch settings for this branch."));

        dbContext.ItemBranchSettings.Remove(setting);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
