namespace ZARI.Application.Features.Sales.DiscountRules.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateDiscountRuleCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<UpdateDiscountRuleCommand>
{
    public async Task<Result> HandleAsync(UpdateDiscountRuleCommand command, CancellationToken cancellationToken = default)
    {
        var rule = await dbContext.DiscountRules.FindAsync([command.Id], cancellationToken);
        if (rule is null)
            return Result.Failure(Error.NotFound("DiscountRule.NotFound", $"Discount rule with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("DISCOUNT_RULES", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("DiscountRule.Forbidden", "You do not have permission to update discount rules."));

        var duplicateCode = await dbContext.DiscountRules
            .AnyAsync(r => r.Id != command.Id && r.Code == command.Code, cancellationToken);
        if (duplicateCode)
            return Result.Failure(Error.Conflict("DiscountRule.DuplicateCode", $"A discount rule with code '{command.Code}' already exists."));

        if (command.ItemId is not null)
        {
            var itemExists = await dbContext.Items.AnyAsync(i => i.Id == command.ItemId, cancellationToken);
            if (!itemExists)
                return Result.Failure(Error.NotFound("Item.NotFound", $"Item with ID '{command.ItemId}' was not found."));
        }

        if (command.ItemCategoryId is not null)
        {
            var categoryExists = await dbContext.ItemCategories.AnyAsync(c => c.Id == command.ItemCategoryId, cancellationToken);
            if (!categoryExists)
                return Result.Failure(Error.NotFound("ItemCategory.NotFound", $"Item category with ID '{command.ItemCategoryId}' was not found."));
        }

        if (command.BranchId is not null)
        {
            var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
            if (!branchExists)
                return Result.Failure(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));
        }

        rule.Code = command.Code;
        rule.Name = command.Name;
        rule.Scope = command.Scope;
        rule.ItemId = command.ItemId;
        rule.ItemCategoryId = command.ItemCategoryId;
        rule.DiscountType = command.DiscountType;
        rule.DiscountValue = command.DiscountValue;
        rule.MinQty = command.MinQty;
        rule.StartDate = command.StartDate;
        rule.EndDate = command.EndDate;
        rule.BranchId = command.BranchId;
        rule.Priority = command.Priority;
        rule.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
