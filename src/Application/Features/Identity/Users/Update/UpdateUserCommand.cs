namespace ZARI.Application.Features.Identity.Users.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateUserCommand(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    string Status,
    List<string> RoleIds,
    List<string> BranchIds) : ICommand;
