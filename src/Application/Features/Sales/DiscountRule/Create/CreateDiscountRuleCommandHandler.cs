namespace ZARI.Application.Features.Sales.DiscountRules.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.DiscountRules.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateDiscountRuleCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<CreateDiscountRuleCommand, Result<DiscountRuleResponse>>
{
    public async Task<Result<DiscountRuleResponse>> HandleAsync(CreateDiscountRuleCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("DISCOUNT_RULES", FormAction.Create, cancellationToken))
            return Result.Failure<DiscountRuleResponse>(Error.Forbidden("DiscountRule.Forbidden", "You do not have permission to create discount rules."));

        var codeExists = await dbContext.DiscountRules.AnyAsync(r => r.Code == command.Code, cancellationToken);
        if (codeExists)
            return Result.Failure<DiscountRuleResponse>(Error.Conflict("DiscountRule.DuplicateCode", $"A discount rule with code '{command.Code}' already exists."));

        var itemIds = (command.ItemIds ?? []).Distinct().ToList();
        if (itemIds.Count > 0)
        {
            var foundCount = await dbContext.Items.CountAsync(i => itemIds.Contains(i.Id), cancellationToken);
            if (foundCount != itemIds.Count)
                return Result.Failure<DiscountRuleResponse>(Error.NotFound("Item.NotFound", "One or more items on this discount rule were not found."));
        }

        if (command.ItemCategoryId is not null)
        {
            var categoryExists = await dbContext.ItemCategories.AnyAsync(c => c.Id == command.ItemCategoryId, cancellationToken);
            if (!categoryExists)
                return Result.Failure<DiscountRuleResponse>(Error.NotFound("ItemCategory.NotFound", $"Item category with ID '{command.ItemCategoryId}' was not found."));
        }

        if (command.BranchId is not null)
        {
            var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
            if (!branchExists)
                return Result.Failure<DiscountRuleResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));
        }

        var rule = new DiscountRule
        {
            Code = command.Code,
            Name = command.Name,
            Scope = command.Scope,
            Items = itemIds.Select(id => new DiscountRuleItem { ItemId = id }).ToList(),
            ItemCategoryId = command.ItemCategoryId,
            DiscountType = command.DiscountType,
            DiscountValue = command.DiscountValue,
            MinQty = command.MinQty,
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            BranchId = command.BranchId,
            Priority = command.Priority,
            Status = command.Status
        };

        dbContext.DiscountRules.Add(rule);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new DiscountRuleResponse(rule.Id, rule.Code, rule.Name, rule.Scope, rule.Items.Select(i => i.ItemId).ToList(), rule.ItemCategoryId, rule.DiscountType, rule.DiscountValue,
            rule.MinQty, rule.StartDate, rule.EndDate, rule.BranchId, rule.Priority, rule.Status, rule.CreatedAt);
        return Result.Success(response);
    }
}
