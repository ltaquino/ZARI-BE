namespace ZARI.Application.Features.System.DocumentSequences.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteDocumentSequenceCommand(Guid Id) : ICommand;
