namespace ZARI.Application.Features.Inventory.GoodsIssues.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Abstractions.Identity;
using ZARI.Domain.Common;

public sealed class DeleteGoodsIssueCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteGoodsIssueCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteGoodsIssueCommand command, CancellationToken cancellationToken = default)
    {
        var issue = await dbContext.GoodsIssues.FindAsync([command.Id], cancellationToken);
        if (issue is null)
            return Result.Failure(Error.NotFound("GoodsIssue.NotFound", $"Goods issue with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("GOODS_ISSUES", FormAction.Delete, issue.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("GoodsIssue.Forbidden", "You do not have permission to delete goods issues for this branch."));

        if (issue.Status != "DRAFT")
            return Result.Failure(Error.Validation("GoodsIssue.NotDraft", "Only draft goods issues can be deleted — cancel it instead."));

        dbContext.GoodsIssues.Remove(issue);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
