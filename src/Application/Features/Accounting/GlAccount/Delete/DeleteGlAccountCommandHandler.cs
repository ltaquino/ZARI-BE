namespace ZARI.Application.Features.Accounting.GlAccounts.Delete;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteGlAccountCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteGlAccountCommand>
{
    public async Task<Result> HandleAsync(DeleteGlAccountCommand command, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.GlAccounts.FindAsync([command.Id], cancellationToken);
        if (account is null)
            return Result.Failure(Error.NotFound("GlAccount.NotFound", $"GL account with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("GL_ACCOUNTS", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("GlAccount.Forbidden", "You do not have permission to delete GL accounts."));

        var childCount = await dbContext.GlAccounts.CountAsync(a => a.ParentAccountId == command.Id, cancellationToken);
        if (childCount > 0)
        {
            return Result.Failure(Error.Conflict(
                "GlAccount.HasChildren",
                $"Cannot delete this account — it has {childCount} child account{(childCount == 1 ? "" : "s")}."));
        }

        dbContext.GlAccounts.Remove(account);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
