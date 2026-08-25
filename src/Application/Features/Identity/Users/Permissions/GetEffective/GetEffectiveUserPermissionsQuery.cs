namespace ZARI.Application.Features.Identity.Users.Permissions.GetEffective;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Identity.Permissions.Shared;
using ZARI.Domain.Common;

public sealed record GetEffectiveUserPermissionsQuery(string UserId) : IQuery<Result<List<FormPermissionResponse>>>;
