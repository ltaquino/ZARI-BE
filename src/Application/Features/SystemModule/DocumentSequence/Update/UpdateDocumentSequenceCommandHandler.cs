namespace ZARI.Application.Features.SystemModule.DocumentSequences.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateDocumentSequenceCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<UpdateDocumentSequenceCommand>
{
    public async Task<Result> HandleAsync(UpdateDocumentSequenceCommand command, CancellationToken cancellationToken = default)
    {
        var sequence = await dbContext.DocumentSequences.FindAsync([command.Id], cancellationToken);
        if (sequence is null)
            return Result.Failure(Error.NotFound("DocumentSequence.NotFound", $"Document sequence with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("DOCUMENT_SEQUENCES", FormAction.Edit, sequence.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("DocumentSequence.Forbidden", "You do not have permission to update document sequences for this branch."));

        var clashExists = await dbContext.DocumentSequences
            .AnyAsync(s => s.Id != command.Id && s.BranchId == command.BranchId && s.DocType == command.DocType, cancellationToken);

        if (clashExists)
            return Result.Failure(Error.Conflict("DocumentSequence.Duplicate", "A sequence for this branch and document type already exists — edit it instead."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        sequence.BranchId = command.BranchId;
        sequence.DocType = command.DocType;
        sequence.Prefix = command.Prefix;
        sequence.NextNumber = command.NextNumber;
        sequence.PaddingLength = command.PaddingLength;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
