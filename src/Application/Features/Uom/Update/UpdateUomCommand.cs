namespace ZARI.Application.Features.Uoms.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateUomCommand(Guid Id, string Code, string Name) : ICommand;
