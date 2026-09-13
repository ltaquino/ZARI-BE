namespace ZARI.Application.Features.Hr.Positions.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeletePositionCommand(Guid Id) : ICommand;
