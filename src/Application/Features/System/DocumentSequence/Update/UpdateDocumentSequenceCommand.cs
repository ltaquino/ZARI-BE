namespace ZARI.Application.Features.System.DocumentSequences.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateDocumentSequenceCommand(
    Guid Id,
    string BranchId,
    string DocType,
    string Prefix,
    int NextNumber,
    int PaddingLength) : ICommand;
