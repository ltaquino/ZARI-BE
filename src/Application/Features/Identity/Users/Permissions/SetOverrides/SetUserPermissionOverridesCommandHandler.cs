namespace ZARI.Application.Features.Identity.Users.Permissions.SetOverrides;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class SetUserPermissionOverridesCommandHandler(
    UserManager<ApplicationUser> userManager,
    IAppDbContext dbContext,
    IPermissionService permissionService) : ICommandHandler<SetUserPermissionOverridesCommand>
{
    public async Task<Result> HandleAsync(SetUserPermissionOverridesCommand command, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null)
            return Result.Failure(Error.NotFound("User.NotFound", $"User with ID '{command.UserId}' was not found."));

        if (!await permissionService.HasPermissionAsync("USERS", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("User.Forbidden", "You do not have permission to change user permission overrides."));

        var formCodes = command.Overrides.Select(o => o.FormCode).Distinct().ToList();
        var formCount = await dbContext.Forms.CountAsync(f => formCodes.Contains(f.Code), cancellationToken);
        if (formCount != formCodes.Count)
            return Result.Failure(Error.NotFound("Form.NotFound", "One or more forms were not found."));

        await dbContext.UserFormPermissionOverrides.Where(o => o.UserId == command.UserId).ExecuteDeleteAsync(cancellationToken);

        var newOverrides = command.Overrides.Select(o => new UserFormPermissionOverride
        {
            UserId = command.UserId,
            FormCode = o.FormCode,
            CanView = o.CanView,
            CanCreate = o.CanCreate,
            CanEdit = o.CanEdit,
            CanApprove = o.CanApprove,
            CanCancel = o.CanCancel,
            CanDelete = o.CanDelete
        });

        dbContext.UserFormPermissionOverrides.AddRange(newOverrides);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
