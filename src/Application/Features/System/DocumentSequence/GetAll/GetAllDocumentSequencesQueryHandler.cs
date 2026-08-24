namespace ZARI.Application.Features.System.DocumentSequences.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.System.DocumentSequences.Get;
using ZARI.Domain.Common;

public sealed class GetAllDocumentSequencesQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetAllDocumentSequencesQuery, Result<List<DocumentSequenceResponse>>>
{
    public async Task<Result<List<DocumentSequenceResponse>>> HandleAsync(GetAllDocumentSequencesQuery query, CancellationToken cancellationToken = default)
    {
        var sequences = await dbContext.DocumentSequences
            .OrderBy(s => s.DocType)
            .ThenBy(s => s.BranchId)
            .Select(s => new DocumentSequenceResponse(s.Id, s.BranchId, s.DocType, s.Prefix, s.NextNumber, s.PaddingLength, s.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(sequences);
    }
}
