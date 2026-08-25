namespace ZARI.Application.Features.SystemModule.DocumentSequences.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteDocumentSequenceCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteDocumentSequenceCommand>
{
    public async Task<Result> HandleAsync(DeleteDocumentSequenceCommand command, CancellationToken cancellationToken = default)
    {
        var sequence = await dbContext.DocumentSequences.FindAsync([command.Id], cancellationToken);
        if (sequence is null)
            return Result.Failure(Error.NotFound("DocumentSequence.NotFound", $"Document sequence with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("DOCUMENT_SEQUENCES", FormAction.Delete, sequence.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("DocumentSequence.Forbidden", "You do not have permission to delete document sequences for this branch."));

        dbContext.DocumentSequences.Remove(sequence);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
