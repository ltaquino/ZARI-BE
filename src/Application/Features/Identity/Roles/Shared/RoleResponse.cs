namespace ZARI.Application.Features.Identity.Roles.Shared;

using ZARI.Application.Features.Identity.Permissions.Shared;

public sealed record RoleResponse(string Id, string Name, List<FormPermissionResponse> Permissions);
