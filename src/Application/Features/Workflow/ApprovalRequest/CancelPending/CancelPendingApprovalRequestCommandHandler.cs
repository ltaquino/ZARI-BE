namespace ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>
/// A no-op (not an error) if there's no pending request for this entity — mirrors the FE
/// prototype's cancelPendingApprovalRequest, which is called defensively from every document's
/// draft-cancel path regardless of whether a request was ever actually submitted.
/// </summary>
public sealed class CancelPendingApprovalRequestCommandHandler(IAppDbContext dbContext) : ICommandHandler<CancelPendingApprovalRequestCommand, Result>
{
    public async Task<Result> HandleAsync(CancelPendingApprovalRequestCommand command, CancellationToken cancellationToken = default)
    {
        var latest = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == command.EntityType && r.EntityId == command.EntityId)
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is null || latest.Status != "PENDING")
            return Result.Success();

        await dbContext.ApprovalRequests
            .Where(r => r.Id == latest.Id && r.Status == "PENDING")
            .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.Status, "CANCELLED"), cancellationToken);

        return Result.Success();
    }
}
