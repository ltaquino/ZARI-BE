namespace ZARI.Application.Features.Identity.Users.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Identity.Users.Shared;
using ZARI.Domain.Common;

public sealed record GetUserQuery(string Id) : IQuery<Result<UserResponse>>;
