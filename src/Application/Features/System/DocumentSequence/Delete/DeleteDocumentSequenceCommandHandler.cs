namespace ZARI.Application.Features.System.DocumentSequences.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteDocumentSequenceCommandHandler(IAppDbContext dbContext) : ICommandHandler<DeleteDocumentSequenceCommand>
{
    public async Task<Result> HandleAsync(DeleteDocumentSequenceCommand command, CancellationToken cancellationToken = default)
    {
        var sequence = await dbContext.DocumentSequences.FindAsync([command.Id], cancellationToken);
        if (sequence is null)
            return Result.Failure(Error.NotFound("DocumentSequence.NotFound", $"Document sequence with ID '{command.Id}' was not found."));

        dbContext.DocumentSequences.Remove(sequence);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
