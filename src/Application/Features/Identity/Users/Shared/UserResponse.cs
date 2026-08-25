namespace ZARI.Application.Features.Identity.Users.Shared;

public sealed record UserResponse(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    string Status,
    List<string> RoleIds,
    List<string> RoleNames,
    List<string> BranchIds);
