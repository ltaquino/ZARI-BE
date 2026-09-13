namespace ZARI.Application.Features.Hr.Positions.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.Positions.Get;
using ZARI.Domain.Common;

public sealed record CreatePositionCommand(
    string Code,
    string Name,
    Guid DepartmentId,
    string Status,
    decimal? PayGradeMin,
    decimal? PayGradeMax) : ICommand<Result<PositionResponse>>;
