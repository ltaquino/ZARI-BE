namespace ZARI.Application.Features.SystemModule.DocumentSequences.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetDocumentSequenceQuery(Guid Id) : IQuery<Result<DocumentSequenceResponse>>;

public sealed record DocumentSequenceResponse(
    Guid Id,
    string BranchId,
    string DocType,
    string Prefix,
    int NextNumber,
    int PaddingLength,
    DateTimeOffset CreatedAt);
