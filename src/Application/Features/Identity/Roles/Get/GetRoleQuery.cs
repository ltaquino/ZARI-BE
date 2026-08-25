namespace ZARI.Application.Features.Identity.Roles.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Identity.Roles.Shared;
using ZARI.Domain.Common;

public sealed record GetRoleQuery(string Id) : IQuery<Result<RoleResponse>>;
