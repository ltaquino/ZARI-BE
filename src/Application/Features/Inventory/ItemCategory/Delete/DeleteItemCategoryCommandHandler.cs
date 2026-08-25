namespace ZARI.Application.Features.Inventory.ItemCategories.Delete;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteItemCategoryCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteItemCategoryCommand>
{
    public async Task<Result> HandleAsync(DeleteItemCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.ItemCategories.FindAsync([command.Id], cancellationToken);
        if (category is null)
            return Result.Failure(Error.NotFound("ItemCategory.NotFound", $"Item category with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("ITEM_CATEGORIES", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("ItemCategory.Forbidden", "You do not have permission to delete item categories."));

        var childCount = await dbContext.ItemCategories.CountAsync(c => c.ParentCategoryId == command.Id, cancellationToken);
        if (childCount > 0)
            return Result.Failure(Error.Conflict("ItemCategory.HasChildren", $"Cannot delete this category — it has {childCount} child categor{(childCount == 1 ? "y" : "ies")}."));

        var itemCount = await dbContext.Items.CountAsync(i => i.CategoryId == command.Id, cancellationToken);
        if (itemCount > 0)
            return Result.Failure(Error.Conflict("ItemCategory.InUse", $"Cannot delete this category — it is used by {itemCount} item{(itemCount == 1 ? "" : "s")}."));

        dbContext.ItemCategories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
