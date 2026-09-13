namespace ZARI.Application.Features.Hr.Positions.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdatePositionCommand(
    Guid Id,
    string Code,
    string Name,
    Guid DepartmentId,
    string Status,
    decimal? PayGradeMin,
    decimal? PayGradeMax) : ICommand;
