namespace ZARI.Application.Features.Identity.Users.Shared;

using Microsoft.AspNetCore.Identity;
using ZARI.Domain.Entities;

// Shared by Get/GetAll/Create/Update so all four assemble the same shape from the same pre-fetched
// lookups (roles, branch assignments) rather than each re-querying for a single user.
public static class UserResponseFactory
{
    public static UserResponse Build(
        ApplicationUser user,
        IList<string> roleNames,
        List<IdentityRole> allRoles,
        List<UserBranch> userBranches)
    {
        var roleIds = allRoles.Where(r => r.Name is not null && roleNames.Contains(r.Name)).Select(r => r.Id).ToList();
        var branchIds = userBranches.Where(ub => ub.UserId == user.Id).Select(ub => ub.BranchId).ToList();

        return new UserResponse(user.Id, user.Email!, user.FirstName, user.LastName, user.Phone, user.Status, roleIds, roleNames.ToList(), branchIds);
    }
}
