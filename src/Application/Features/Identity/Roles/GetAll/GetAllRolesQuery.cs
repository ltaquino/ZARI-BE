namespace ZARI.Application.Features.Identity.Roles.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Identity.Roles.Shared;
using ZARI.Domain.Common;

public sealed record GetAllRolesQuery : IQuery<Result<List<RoleResponse>>>;
