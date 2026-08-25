namespace ZARI.Application.Features.SystemModule.DocumentSequences.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetDocumentSequenceQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetDocumentSequenceQuery, Result<DocumentSequenceResponse>>
{
    public async Task<Result<DocumentSequenceResponse>> HandleAsync(GetDocumentSequenceQuery query, CancellationToken cancellationToken = default)
    {
        var sequence = await dbContext.DocumentSequences
            .Where(s => s.Id == query.Id)
            .Select(s => new DocumentSequenceResponse(s.Id, s.BranchId, s.DocType, s.Prefix, s.NextNumber, s.PaddingLength, s.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (sequence is null)
            return Result.Failure<DocumentSequenceResponse>(Error.NotFound("DocumentSequence.NotFound", $"Document sequence with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("DOCUMENT_SEQUENCES", FormAction.View, sequence.BranchId, cancellationToken))
            return Result.Failure<DocumentSequenceResponse>(Error.Forbidden("DocumentSequence.Forbidden", "You do not have permission to view document sequences for this branch."));

        return Result.Success(sequence);
    }
}
