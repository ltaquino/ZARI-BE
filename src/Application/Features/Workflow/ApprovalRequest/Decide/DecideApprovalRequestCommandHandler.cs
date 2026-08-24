namespace ZARI.Application.Features.Workflow.ApprovalRequests.Decide;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Enforces segregation of duties (the requester cannot decide their own request) and decides the
/// request atomically: an "UPDATE ... WHERE Id = ? AND Status = 'PENDING'" compare-and-swap, the
/// same pattern GetNextDocumentNumberCommandHandler uses — if two managers race to decide the same
/// request, only the first UPDATE's WHERE clause still matches, so the second gets 0 rows affected
/// and a clean conflict instead of silently double-deciding. Unlike DocumentSequence this never
/// needs to retry: a lost race is a real conflict to report, not a transient collision to retry past.
/// </summary>
public sealed class DecideApprovalRequestCommandHandler(IAppDbContext dbContext) : ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>
{
    public async Task<Result<ApprovalRequestResponse>> HandleAsync(DecideApprovalRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.ApprovalRequests.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (request is null)
            return Result.Failure<ApprovalRequestResponse>(Error.NotFound("ApprovalRequest.NotFound", $"Approval request with ID '{command.Id}' was not found."));

        if (request.RequestedBy == command.ApproverUserId)
        {
            return Result.Failure<ApprovalRequestResponse>(Error.Validation(
                "ApprovalRequest.SelfApproval", "You cannot approve or reject your own submission."));
        }

        var newStatus = command.Action == "Approve" ? "APPROVED" : "REJECTED";

        var rowsAffected = await dbContext.ApprovalRequests
            .Where(r => r.Id == command.Id && r.Status == "PENDING")
            .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.Status, newStatus), cancellationToken);

        if (rowsAffected == 0)
        {
            return Result.Failure<ApprovalRequestResponse>(Error.Conflict(
                "ApprovalRequest.AlreadyDecided", "This approval request has already been decided."));
        }

        dbContext.ApprovalActions.Add(new ApprovalAction
        {
            ApprovalRequestId = command.Id,
            ApproverUserId = command.ApproverUserId,
            Action = command.Action,
            ActionAt = DateTimeOffset.UtcNow,
            Comments = command.Comments
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await dbContext.ApprovalRequests.AsNoTracking()
            .Include(r => r.Actions)
            .FirstAsync(r => r.Id == command.Id, cancellationToken);

        return Result.Success(ApprovalRequestMapper.ToResponse(updated));
    }
}
