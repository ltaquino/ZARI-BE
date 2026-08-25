namespace ZARI.Application.Features.Identity.Roles.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Identity.Permissions.Shared;
using ZARI.Application.Features.Identity.Roles.Shared;
using ZARI.Domain.Common;

public sealed record CreateRoleCommand(string Name, List<FormPermissionInput> Permissions) : ICommand<Result<RoleResponse>>;
