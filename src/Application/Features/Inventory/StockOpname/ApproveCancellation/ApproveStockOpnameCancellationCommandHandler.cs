namespace ZARI.Application.Features.Inventory.StockOpnames.ApproveCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Application.Features.Inventory.StockOpnames.GetAll;
using ZARI.Application.Features.Inventory.StockOpnames.Shared;
using ZARI.Application.Features.Inventory.SerialNumbers.ReverseIssue;
using ZARI.Application.Features.Inventory.SerialNumbers.ReverseReceive;
using ZARI.Application.Features.Inventory.StockLedgers.Reverse;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_CANCELLATION -> CANCELLED. Only an HQ admin may finalize the reversal of a posted
/// document. Mirrors the FE prototype's approveStockOpnameCancellation: reverse the stock ledger
/// movements (one batch call handles both the receive-like and issue-like lines), reverse any
/// serials moved, reverse the posted GL journal(s), then decide the cancellation request.
/// </summary>
public sealed class ApproveStockOpnameCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<ReverseStockMovementsCommand, Result> reverseStockHandler,
    ICommandHandler<ReverseReceiveSerialCommand, Result> reverseReceiveSerialHandler,
    ICommandHandler<ReverseIssueSerialCommand, Result> reverseIssueSerialHandler,
    ICommandHandler<ReverseGlJournalsCommand, Result<List<GlJournalResponse>>> reverseGlJournalsHandler,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveStockOpnameCancellationCommand, Result<StockOpnameResponse>>
{
    public async Task<Result<StockOpnameResponse>> HandleAsync(ApproveStockOpnameCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var opname = await dbContext.StockOpnames
            .Include(o => o.Lines).ThenInclude(l => l.Item).ThenInclude(i => i.BaseUom)
            .FirstOrDefaultAsync(o => o.Id == command.Id, cancellationToken);

        if (opname is null)
            return Result.Failure<StockOpnameResponse>(Error.NotFound("StockOpname.NotFound", $"Stock opname with ID '{command.Id}' was not found."));

        if (!await permissionService.HasCancellationAuthorityAsync("STOCK_OPNAMES", cancellationToken))
            return Result.Failure<StockOpnameResponse>(Error.Forbidden("StockOpname.Forbidden", "Only someone with cancel permission assigned to the head office branch can decide a cancellation request."));

        if (opname.Status != "PENDING_CANCELLATION")
            return Result.Failure<StockOpnameResponse>(Error.Validation("StockOpname.NotPendingCancellation", "Only a stock count pending cancellation can be cancelled this way."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "STOCK_OPNAME" && r.EntityId == opname.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<StockOpnameResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this stock count."));

        var lineIds = opname.Lines.Select(l => l.Id.ToString()).ToList();
        var reverseStockResult = await reverseStockHandler.HandleAsync(new ReverseStockMovementsCommand("StockOpnameLine", lineIds), cancellationToken);
        if (!reverseStockResult.IsSuccess)
            return Result.Failure<StockOpnameResponse>(reverseStockResult.Error!);

        foreach (var line in opname.Lines)
        {
            if (!line.Item.IsSerialized || string.IsNullOrWhiteSpace(line.SerialNo)) continue;

            if (line.VarianceQty > 0.0001m)
            {
                var reverseResult = await reverseReceiveSerialHandler.HandleAsync(new ReverseReceiveSerialCommand(line.ItemId, line.SerialNo, "REMOVE"), cancellationToken);
                if (!reverseResult.IsSuccess)
                    return Result.Failure<StockOpnameResponse>(reverseResult.Error!);
            }
            else if (line.VarianceQty < -0.0001m)
            {
                var reverseResult = await reverseIssueSerialHandler.HandleAsync(new ReverseIssueSerialCommand(line.ItemId, line.SerialNo), cancellationToken);
                if (!reverseResult.IsSuccess)
                    return Result.Failure<StockOpnameResponse>(reverseResult.Error!);
            }
        }

        var reverseJournalsResult = await reverseGlJournalsHandler.HandleAsync(
            new ReverseGlJournalsCommand("StockOpname", opname.Id.ToString(), DateTimeOffset.UtcNow, $"Cancellation of {opname.OpnameNo}"), cancellationToken);
        if (!reverseJournalsResult.IsSuccess)
            return Result.Failure<StockOpnameResponse>(reverseJournalsResult.Error!);

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<StockOpnameResponse>(decideResult.Error!);

        // ReverseStockMovementsCommand runs its own retryable transaction and calls
        // ChangeTracker.Clear() at the start of every attempt — that detaches the `opname` this
        // handler loaded earlier, so mutating it and calling SaveChangesAsync would silently
        // persist nothing. ExecuteUpdateAsync writes directly, independent of the tracker.
        var cancelledAt = DateTimeOffset.UtcNow;
        opname.Status = "CANCELLED";
        opname.CancelledBy = command.ApproverUserId;
        opname.CancelledAt = cancelledAt;
        await dbContext.StockOpnames.Where(o => o.Id == opname.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.Status, "CANCELLED")
                .SetProperty(o => o.CancelledBy, command.ApproverUserId)
                .SetProperty(o => o.CancelledAt, cancelledAt), cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("STOCK_OPNAME", opname.Id.ToString(), opname.BranchId, "CANCELLATION_APPROVED", "ACTIVITY",
                "approved the cancellation request", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<StockOpnameResponse>(notifyResult.Error!);

        return Result.Success(StockOpnameMapper.ToResponse(opname));
    }
}
