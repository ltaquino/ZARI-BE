namespace ZARI.Application.Features.Identity.Users.Get;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Identity.Users.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetUserQueryHandler(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IAppDbContext dbContext,
    ICurrentUser currentUser,
    IPermissionService permissionService) : IQueryHandler<GetUserQuery, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> HandleAsync(GetUserQuery query, CancellationToken cancellationToken = default)
    {
        var isSelf = query.Id == currentUser.UserId;
        if (!isSelf && !await permissionService.HasPermissionAsync("USERS", FormAction.View, cancellationToken))
            return Result.Failure<UserResponse>(Error.Forbidden("User.Forbidden", "You do not have permission to view users."));

        var user = await userManager.FindByIdAsync(query.Id);
        if (user is null)
            return Result.Failure<UserResponse>(Error.NotFound("User.NotFound", $"User with ID '{query.Id}' was not found."));

        var roleNames = await userManager.GetRolesAsync(user);
        var allRoles = await roleManager.Roles.ToListAsync(cancellationToken);
        var userBranches = await dbContext.UserBranches.Where(ub => ub.UserId == user.Id).ToListAsync(cancellationToken);

        return Result.Success(UserResponseFactory.Build(user, roleNames, allRoles, userBranches));
    }
}
