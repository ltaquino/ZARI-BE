namespace ZARI.Application.Features.Identity.Users.Update;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IAppDbContext dbContext,
    IPermissionService permissionService) : ICommandHandler<UpdateUserCommand>
{
    public async Task<Result> HandleAsync(UpdateUserCommand command, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(command.Id);
        if (user is null)
            return Result.Failure(Error.NotFound("User.NotFound", $"User with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("USERS", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("User.Forbidden", "You do not have permission to update users."));

        if (!string.Equals(user.Email, command.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailOwner = await userManager.FindByEmailAsync(command.Email);
            if (emailOwner is not null && emailOwner.Id != user.Id)
                return Result.Failure(Error.Conflict("User.EmailTaken", "A user with this email already exists."));
        }

        var roles = await roleManager.Roles.Where(r => command.RoleIds.Contains(r.Id)).ToListAsync(cancellationToken);
        if (roles.Count != command.RoleIds.Distinct().Count())
            return Result.Failure(Error.NotFound("Role.NotFound", "One or more roles were not found."));

        var branchCount = await dbContext.Branches.CountAsync(b => command.BranchIds.Contains(b.Id), cancellationToken);
        if (branchCount != command.BranchIds.Distinct().Count())
            return Result.Failure(Error.NotFound("Branch.NotFound", "One or more branches were not found."));

        user.Email = command.Email;
        user.UserName = command.Email;
        user.FirstName = command.FirstName;
        user.LastName = command.LastName;
        user.Phone = command.Phone;
        user.Status = command.Status;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return Result.Failure(Error.Validation("User.UpdateFailed", errors));
        }

        var currentRoleNames = await userManager.GetRolesAsync(user);
        var desiredRoleNames = roles.Select(r => r.Name!).ToList();
        var rolesToRemove = currentRoleNames.Except(desiredRoleNames).ToList();
        var rolesToAdd = desiredRoleNames.Except(currentRoleNames).ToList();
        if (rolesToRemove.Count > 0)
            await userManager.RemoveFromRolesAsync(user, rolesToRemove);
        if (rolesToAdd.Count > 0)
            await userManager.AddToRolesAsync(user, rolesToAdd);

        await dbContext.UserBranches.Where(ub => ub.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);
        var newBranches = command.BranchIds.Distinct().Select(branchId => new UserBranch { UserId = user.Id, BranchId = branchId });
        dbContext.UserBranches.AddRange(newBranches);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
