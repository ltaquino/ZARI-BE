namespace ZARI.Application.Features.Identity.Users.Create;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Identity.Users.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IAppDbContext dbContext,
    IPermissionService permissionService) : ICommandHandler<CreateUserCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("USERS", FormAction.Create, cancellationToken))
            return Result.Failure<UserResponse>(Error.Forbidden("User.Forbidden", "You do not have permission to create users."));

        var existingUser = await userManager.FindByEmailAsync(command.Email);
        if (existingUser is not null)
            return Result.Failure<UserResponse>(Error.Conflict("User.EmailTaken", "A user with this email already exists."));

        var roles = await roleManager.Roles.Where(r => command.RoleIds.Contains(r.Id)).ToListAsync(cancellationToken);
        if (roles.Count != command.RoleIds.Distinct().Count())
            return Result.Failure<UserResponse>(Error.NotFound("Role.NotFound", "One or more roles were not found."));

        var branchCount = await dbContext.Branches.CountAsync(b => command.BranchIds.Contains(b.Id), cancellationToken);
        if (branchCount != command.BranchIds.Distinct().Count())
            return Result.Failure<UserResponse>(Error.NotFound("Branch.NotFound", "One or more branches were not found."));

        var user = new ApplicationUser
        {
            FirstName = command.FirstName,
            LastName = command.LastName,
            Email = command.Email,
            UserName = command.Email,
            Phone = command.Phone,
            Status = command.Status,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, command.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return Result.Failure<UserResponse>(Error.Validation("User.CreateFailed", errors));
        }

        var roleNames = roles.Select(r => r.Name!).ToList();
        if (roleNames.Count > 0)
            await userManager.AddToRolesAsync(user, roleNames);

        var userBranches = command.BranchIds.Distinct().Select(branchId => new UserBranch { UserId = user.Id, BranchId = branchId }).ToList();
        dbContext.UserBranches.AddRange(userBranches);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = UserResponseFactory.Build(user, roleNames, roles, userBranches);
        return Result.Success(response);
    }
}
