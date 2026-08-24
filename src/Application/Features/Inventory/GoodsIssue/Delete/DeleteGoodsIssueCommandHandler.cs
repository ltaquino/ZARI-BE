namespace ZARI.Application.Features.Inventory.GoodsIssues.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteGoodsIssueCommandHandler(IAppDbContext dbContext) : ICommandHandler<DeleteGoodsIssueCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteGoodsIssueCommand command, CancellationToken cancellationToken = default)
    {
        var issue = await dbContext.GoodsIssues.FindAsync([command.Id], cancellationToken);
        if (issue is null)
            return Result.Failure(Error.NotFound("GoodsIssue.NotFound", $"Goods issue with ID '{command.Id}' was not found."));

        if (issue.Status != "DRAFT")
            return Result.Failure(Error.Validation("GoodsIssue.NotDraft", "Only draft goods issues can be deleted — cancel it instead."));

        dbContext.GoodsIssues.Remove(issue);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
