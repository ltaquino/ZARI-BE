namespace ZARI.Application.Features.Uoms.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteUomCommand(Guid Id) : ICommand;
