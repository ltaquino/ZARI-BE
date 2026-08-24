namespace ZARI.Application.Features.SystemModule.DocumentSequences.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.SystemModule.DocumentSequences.Get;
using ZARI.Domain.Common;

public sealed record CreateDocumentSequenceCommand(
    string BranchId,
    string DocType,
    string Prefix,
    int NextNumber,
    int PaddingLength) : ICommand<Result<DocumentSequenceResponse>>;
