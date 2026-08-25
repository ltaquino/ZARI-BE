namespace ZARI.Application.Features.Identity.Users.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Identity.Users.Shared;
using ZARI.Domain.Common;

public sealed record CreateUserCommand(
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    string Status,
    string Password,
    List<string> RoleIds,
    List<string> BranchIds) : ICommand<Result<UserResponse>>;
