namespace ZARI.Application.Features.System.DocumentSequences.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.System.DocumentSequences.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateDocumentSequenceCommandHandler(IAppDbContext dbContext) : ICommandHandler<CreateDocumentSequenceCommand, Result<DocumentSequenceResponse>>
{
    public async Task<Result<DocumentSequenceResponse>> HandleAsync(CreateDocumentSequenceCommand command, CancellationToken cancellationToken = default)
    {
        var clashExists = await dbContext.DocumentSequences
            .AnyAsync(s => s.BranchId == command.BranchId && s.DocType == command.DocType, cancellationToken);

        if (clashExists)
            return Result.Failure<DocumentSequenceResponse>(Error.Conflict("DocumentSequence.Duplicate", "A sequence for this branch and document type already exists — edit it instead."));

        var sequence = new DocumentSequence
        {
            BranchId = command.BranchId,
            DocType = command.DocType,
            Prefix = command.Prefix,
            NextNumber = command.NextNumber,
            PaddingLength = command.PaddingLength
        };

        dbContext.DocumentSequences.Add(sequence);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new DocumentSequenceResponse(sequence.Id, sequence.BranchId, sequence.DocType, sequence.Prefix, sequence.NextNumber, sequence.PaddingLength, sequence.CreatedAt);
        return Result.Success(response);
    }
}
