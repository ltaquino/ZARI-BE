namespace ZARI.Application.Features.SystemModule.DocumentSequences.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteDocumentSequenceCommand(Guid Id) : ICommand;
