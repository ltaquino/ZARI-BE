namespace ZARI.Application.Features.Accounting.GlAccounts.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateGlAccountCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<UpdateGlAccountCommand>
{
    public async Task<Result> HandleAsync(UpdateGlAccountCommand command, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.GlAccounts.FindAsync([command.Id], cancellationToken);
        if (account is null)
            return Result.Failure(Error.NotFound("GlAccount.NotFound", $"GL account with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("GL_ACCOUNTS", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("GlAccount.Forbidden", "You do not have permission to update GL accounts."));

        var duplicateCode = await dbContext.GlAccounts
            .AnyAsync(a => a.Id != command.Id && a.Code == command.Code, cancellationToken);
        if (duplicateCode)
            return Result.Failure(Error.Conflict("GlAccount.DuplicateCode", $"A GL account with code '{command.Code}' already exists."));

        if (command.ParentAccountId is { } parentId)
        {
            if (parentId == command.Id)
                return Result.Failure(Error.Validation("GlAccount.InvalidParent", "An account cannot be its own parent."));

            var parentExists = await dbContext.GlAccounts.AnyAsync(a => a.Id == parentId, cancellationToken);
            if (!parentExists)
                return Result.Failure(Error.NotFound("GlAccount.ParentNotFound", $"Parent GL account with ID '{parentId}' was not found."));
        }

        account.Code = command.Code;
        account.Name = command.Name;
        account.AccountType = command.AccountType;
        account.NormalBalance = command.NormalBalance;
        account.ParentAccountId = command.ParentAccountId;
        account.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
