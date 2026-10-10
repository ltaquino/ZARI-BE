namespace ZARI.Application.Features.SystemModule.DocumentSequences.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.SystemModule.DocumentSequences.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateDocumentSequenceCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<CreateDocumentSequenceCommand, Result<DocumentSequenceResponse>>
{
    public async Task<Result<DocumentSequenceResponse>> HandleAsync(CreateDocumentSequenceCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("DOCUMENT_SEQUENCES", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<DocumentSequenceResponse>(Error.Forbidden("DocumentSequence.Forbidden", "You do not have permission to create document sequences for this branch."));

        var clashExists = await dbContext.DocumentSequences
            .AnyAsync(s => s.BranchId == command.BranchId && s.DocType == command.DocType, cancellationToken);

        if (clashExists)
            return Result.Failure<DocumentSequenceResponse>(Error.Conflict("DocumentSequence.Duplicate", "A sequence for this branch and document type already exists — edit it instead."));

        // The generated document number is only ever "{Prefix}{NextNumber}" (GetNextDocumentNumberCommandHandler)
        // -- each branch's own NextNumber increments independently and safely, but if two branches share the
        // same Prefix for the same DocType, their counters WILL eventually produce the identical formatted
        // number. For a doc type whose number feeds a globally-unique column (e.g. BIR-OR -> SalesInvoice.
        // BirOrSeriesNumber has a DB-level unique index), that collision surfaces as an unhandled 500 on
        // whichever branch loses the race, not a clean validation error. Guard it here instead.
        var prefixClashBranchId = await dbContext.DocumentSequences
            .Where(s => s.DocType == command.DocType && s.Prefix == command.Prefix && s.BranchId != command.BranchId)
            .Select(s => s.BranchId)
            .FirstOrDefaultAsync(cancellationToken);
        if (prefixClashBranchId is not null)
            return Result.Failure<DocumentSequenceResponse>(Error.Conflict("DocumentSequence.DuplicatePrefix",
                $"Branch '{prefixClashBranchId}' already uses prefix '{command.Prefix}' for {command.DocType} — document numbers must stay unique across branches. Choose a different prefix."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<DocumentSequenceResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

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
