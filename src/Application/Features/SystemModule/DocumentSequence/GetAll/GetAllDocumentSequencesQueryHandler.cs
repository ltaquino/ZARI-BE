namespace ZARI.Application.Features.SystemModule.DocumentSequences.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.SystemModule.DocumentSequences.Get;
using ZARI.Domain.Common;

public sealed class GetAllDocumentSequencesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllDocumentSequencesQuery, Result<List<DocumentSequenceResponse>>>
{
    public async Task<Result<List<DocumentSequenceResponse>>> HandleAsync(GetAllDocumentSequencesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("DOCUMENT_SEQUENCES", FormAction.View, cancellationToken))
            return Result.Failure<List<DocumentSequenceResponse>>(Error.Forbidden("DocumentSequence.Forbidden", "You do not have permission to view document sequences."));

        var sequences = await dbContext.DocumentSequences
            .OrderBy(s => s.DocType)
            .ThenBy(s => s.BranchId)
            .Select(s => new DocumentSequenceResponse(s.Id, s.BranchId, s.DocType, s.Prefix, s.NextNumber, s.PaddingLength, s.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(sequences);
    }
}
