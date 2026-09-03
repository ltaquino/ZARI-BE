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

        var users = await userManager.Users.AsNoTracking()
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .ToListAsync(cancellationToken);

        var allRoles = await roleManager.Roles.AsNoTracking().ToListAsync(cancellationToken);
        var userBranches = await dbContext.UserBranches.AsNoTracking().ToListAsync(cancellationToken);

        var roleNamesByUserId = await dbContext.UserRoles.AsNoTracking()
            .Join(roleManager.Roles.AsNoTracking(), ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .GroupBy(x => x.UserId)
            .ToDictionaryAsync(
                g => g.Key,
                g => (IList<string>)g.Select(x => x.Name!).ToList(),
                cancellationToken);

        var responses = new List<UserResponse>(users.Count);
        foreach (var user in users)
        {
            var roleNames = roleNamesByUserId.TryGetValue(user.Id, out var names) ? names : [];
            responses.Add(UserResponseFactory.Build(user, roleNames, allRoles, userBranches));
        }

        return Result.Success(responses);
    }
}
