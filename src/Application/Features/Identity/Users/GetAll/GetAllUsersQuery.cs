namespace ZARI.Application.Features.Identity.Users.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Identity.Users.Shared;
using ZARI.Domain.Common;

public sealed record GetAllUsersQuery : IQuery<Result<List<UserResponse>>>;
