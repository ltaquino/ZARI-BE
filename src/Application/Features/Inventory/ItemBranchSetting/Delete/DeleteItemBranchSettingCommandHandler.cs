namespace ZARI.Application.Features.Inventory.ItemBranchSettings.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteItemBranchSettingCommandHandler(IAppDbContext dbContext) : ICommandHandler<DeleteItemBranchSettingCommand>
{
    public async Task<Result> HandleAsync(DeleteItemBranchSettingCommand command, CancellationToken cancellationToken = default)
    {
        var setting = await dbContext.ItemBranchSettings.FindAsync([command.Id], cancellationToken);
        if (setting is null)
            return Result.Failure(Error.NotFound("ItemBranchSetting.NotFound", $"Reorder setting with ID '{command.Id}' was not found."));

        dbContext.ItemBranchSettings.Remove(setting);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
