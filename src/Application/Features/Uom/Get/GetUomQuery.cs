namespace ZARI.Application.Features.Uoms.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetUomQuery(Guid Id) : IQuery<Result<UomResponse>>;

public sealed record UomResponse(Guid Id, string Code, string Name, DateTimeOffset CreatedAt);
