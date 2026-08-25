namespace ZARI.Application.Features.Identity.Roles.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Identity.Permissions.Shared;

public sealed record UpdateRoleCommand(string Id, string Name, List<FormPermissionInput> Permissions) : ICommand;
