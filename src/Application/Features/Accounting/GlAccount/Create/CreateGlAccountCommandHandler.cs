namespace ZARI.Application.Features.Accounting.GlAccounts.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlAccounts.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateGlAccountCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<CreateGlAccountCommand, Result<GlAccountResponse>>
{
    public async Task<Result<GlAccountResponse>> HandleAsync(CreateGlAccountCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("GL_ACCOUNTS", FormAction.Create, cancellationToken))
            return Result.Failure<GlAccountResponse>(Error.Forbidden("GlAccount.Forbidden", "You do not have permission to create GL accounts."));

        var codeExists = await dbContext.GlAccounts.AnyAsync(a => a.Code == command.Code, cancellationToken);
        if (codeExists)
            return Result.Failure<GlAccountResponse>(Error.Conflict("GlAccount.DuplicateCode", $"A GL account with code '{command.Code}' already exists."));

        if (command.ParentAccountId is { } parentId)
        {
            var parentExists = await dbContext.GlAccounts.AnyAsync(a => a.Id == parentId, cancellationToken);
            if (!parentExists)
                return Result.Failure<GlAccountResponse>(Error.NotFound("GlAccount.ParentNotFound", $"Parent GL account with ID '{parentId}' was not found."));
        }

        var account = new GlAccount
        {
            Code = command.Code,
            Name = command.Name,
            AccountType = command.AccountType,
            NormalBalance = command.NormalBalance,
            ParentAccountId = command.ParentAccountId,
            Status = command.Status
        };

        dbContext.GlAccounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new GlAccountResponse(account.Id, account.Code, account.Name, account.AccountType, account.NormalBalance, account.ParentAccountId, account.Status, account.CreatedAt);
        return Result.Success(response);
    }
}
