namespace ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetNextDocumentNumberCommand(string BranchId, string DocType) : ICommand<Result<NextDocumentNumberResponse>>;

public sealed record NextDocumentNumberResponse(string DocumentNumber);
