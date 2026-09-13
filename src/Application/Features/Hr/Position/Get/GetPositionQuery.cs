namespace ZARI.Application.Features.Hr.Positions.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetPositionQuery(Guid Id) : IQuery<Result<PositionResponse>>;

public sealed record PositionResponse(
    Guid Id,
    string Code,
    string Name,
    Guid DepartmentId,
    string Status,
    decimal? PayGradeMin,
    decimal? PayGradeMax,
    DateTimeOffset CreatedAt);
