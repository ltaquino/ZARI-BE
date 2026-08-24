namespace ZARI.Application.Features.SystemModule.DocumentSequences.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateDocumentSequenceCommandHandler(IAppDbContext dbContext) : ICommandHandler<UpdateDocumentSequenceCommand>
{
    public async Task<Result> HandleAsync(UpdateDocumentSequenceCommand command, CancellationToken cancellationToken = default)
    {
        var sequence = await dbContext.DocumentSequences.FindAsync([command.Id], cancellationToken);
        if (sequence is null)
            return Result.Failure(Error.NotFound("DocumentSequence.NotFound", $"Document sequence with ID '{command.Id}' was not found."));

        var clashExists = await dbContext.DocumentSequences
            .AnyAsync(s => s.Id != command.Id && s.BranchId == command.BranchId && s.DocType == command.DocType, cancellationToken);

        if (clashExists)
            return Result.Failure(Error.Conflict("DocumentSequence.Duplicate", "A sequence for this branch and document type already exists — edit it instead."));

        sequence.BranchId = command.BranchId;
        sequence.DocType = command.DocType;
        sequence.Prefix = command.Prefix;
        sequence.NextNumber = command.NextNumber;
        sequence.PaddingLength = command.PaddingLength;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
