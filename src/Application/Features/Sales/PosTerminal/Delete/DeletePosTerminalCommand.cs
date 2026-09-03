namespace ZARI.Application.Features.Sales.PosTerminals.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeletePosTerminalCommand(Guid Id) : ICommand;
