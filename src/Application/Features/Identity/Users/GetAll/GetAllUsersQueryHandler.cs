namespace ZARI.Application.Features.Identity.Users.GetAll;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Identity.Users.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetAllUsersQueryHandler(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IAppDbContext dbContext,
    IPermissionService permissionService) : IQueryHandler<GetAllUsersQuery, Result<List<UserResponse>>>
{
    public async Task<Result<List<UserResponse>>> HandleAsync(GetAllUsersQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("USERS", FormAction.View, cancellationToken))
            return Result.Failure<List<UserResponse>>(Error.Forbidden("User.Forbidden", "You do not have permission to view users."));

        var users = await userManager.Users
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .ToListAsync(cancellationToken);

        var allRoles = await roleManager.Roles.ToListAsync(cancellationToken);
        var userBranches = await dbContext.UserBranches.ToListAsync(cancellationToken);

        var responses = new List<UserResponse>(users.Count);
        foreach (var user in users)
        {
            var roleNames = await userManager.GetRolesAsync(user);
            responses.Add(UserResponseFactory.Build(user, roleNames, allRoles, userBranches));
        }

        return Result.Success(responses);
    }
}
