namespace ZARI.Application.Features.System.DocumentSequences.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetDocumentSequenceQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetDocumentSequenceQuery, Result<DocumentSequenceResponse>>
{
    public async Task<Result<DocumentSequenceResponse>> HandleAsync(GetDocumentSequenceQuery query, CancellationToken cancellationToken = default)
    {
        var sequence = await dbContext.DocumentSequences
            .Where(s => s.Id == query.Id)
            .Select(s => new DocumentSequenceResponse(s.Id, s.BranchId, s.DocType, s.Prefix, s.NextNumber, s.PaddingLength, s.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (sequence is null)
            return Result.Failure<DocumentSequenceResponse>(Error.NotFound("DocumentSequence.NotFound", $"Document sequence with ID '{query.Id}' was not found."));

        return Result.Success(sequence);
    }
}
