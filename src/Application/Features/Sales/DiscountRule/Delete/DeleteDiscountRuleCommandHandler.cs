namespace ZARI.Application.Features.Sales.DiscountRules.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteDiscountRuleCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteDiscountRuleCommand>
{
    public async Task<Result> HandleAsync(DeleteDiscountRuleCommand command, CancellationToken cancellationToken = default)
    {
        var rule = await dbContext.DiscountRules.FindAsync([command.Id], cancellationToken);
        if (rule is null)
            return Result.Failure(Error.NotFound("DiscountRule.NotFound", $"Discount rule with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("DISCOUNT_RULES", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("DiscountRule.Forbidden", "You do not have permission to delete discount rules."));

        dbContext.DiscountRules.Remove(rule);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
