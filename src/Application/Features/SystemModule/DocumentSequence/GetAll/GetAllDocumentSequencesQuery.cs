namespace ZARI.Application.Features.SystemModule.DocumentSequences.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.SystemModule.DocumentSequences.Get;
using ZARI.Domain.Common;

public sealed record GetAllDocumentSequencesQuery : IQuery<Result<List<DocumentSequenceResponse>>>;
