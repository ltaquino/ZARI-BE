namespace ZARI.Application.Features.Inventory.ItemCategories.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateItemCategoryCommandHandler(IAppDbContext dbContext) : ICommandHandler<UpdateItemCategoryCommand>
{
    public async Task<Result> HandleAsync(UpdateItemCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.ItemCategories.FindAsync([command.Id], cancellationToken);
        if (category is null)
            return Result.Failure(Error.NotFound("ItemCategory.NotFound", $"Item category with ID '{command.Id}' was not found."));

        var duplicateCode = await dbContext.ItemCategories
            .AnyAsync(c => c.Id != command.Id && c.Code == command.Code, cancellationToken);

        if (duplicateCode)
            return Result.Failure(Error.Conflict("ItemCategory.DuplicateCode", $"An item category with code '{command.Code}' already exists."));

        if (command.ParentCategoryId == command.Id)
            return Result.Failure(Error.Validation("ItemCategory.InvalidParent", "A category cannot be its own parent."));

        if (command.ParentCategoryId is not null)
        {
            var parentExists = await dbContext.ItemCategories
                .AnyAsync(c => c.Id == command.ParentCategoryId, cancellationToken);

            if (!parentExists)
                return Result.Failure(Error.NotFound("ItemCategory.ParentNotFound", $"Parent category with ID '{command.ParentCategoryId}' was not found."));
        }

        category.Code = command.Code;
        category.Name = command.Name;
        category.ParentCategoryId = command.ParentCategoryId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
