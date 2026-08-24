namespace ZARI.Application.Features.System.DocumentSequences.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.System.DocumentSequences.Get;
using ZARI.Domain.Common;

public sealed record GetAllDocumentSequencesQuery : IQuery<Result<List<DocumentSequenceResponse>>>;
