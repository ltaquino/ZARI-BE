namespace ZARI.Application.Features.Sales.SalesReturns.ApproveCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Application.Features.Inventory.StockLedgers.Reverse;
using ZARI.Application.Features.Sales.SalesReturns.GetAll;
using ZARI.Application.Features.Sales.SalesReturns.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_CANCELLATION -> CANCELLED. Only an HQ admin may finalize the reversal of a posted
/// document. Reverse the stock receipt (ReverseStockMovementsCommand handles both the receive and
/// issue directions symmetrically), reverse the posted GL journal (both the stock-value and
/// revenue-side halves live in the one journal SalesReturnPostingService posted), then decide the
/// cancellation request — mirrors ApproveDeliveryOrderCancellationCommandHandler /
/// ApproveGoodsReturnCancellationCommandHandler.
/// </summary>
public sealed class ApproveSalesReturnCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<ReverseStockMovementsCommand, Result> reverseStockHandler,
    ICommandHandler<ReverseGlJournalsCommand, Result<List<GlJournalResponse>>> reverseGlJournalsHandler,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveSalesReturnCancellationCommand, Result<SalesReturnResponse>>
{
    public async Task<Result<SalesReturnResponse>> HandleAsync(ApproveSalesReturnCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var salesReturn = await dbContext.SalesReturns
            .Include(r => r.Customer)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (salesReturn is null)
            return Result.Failure<SalesReturnResponse>(Error.NotFound("SalesReturn.NotFound", $"Sales return with ID '{command.Id}' was not found."));

        if (!await permissionService.HasCancellationAuthorityAsync("SALES_RETURNS", cancellationToken))
            return Result.Failure<SalesReturnResponse>(Error.Forbidden("SalesReturn.Forbidden", "Only someone with cancel permission assigned to the head office branch can decide a cancellation request."));

        if (salesReturn.Status != "PENDING_CANCELLATION")
            return Result.Failure<SalesReturnResponse>(Error.Validation("SalesReturn.NotPendingCancellation", "Only a sales return pending cancellation can be cancelled this way."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "SALES_RETURN" && r.EntityId == salesReturn.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<SalesReturnResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this sales return."));

        // Decide before any reversal side-effect — see ApproveGoodsReceiptCancellationCommandHandler's
        // doc comment for why (a failed decide must leave nothing reversed yet, so it stays retryable).
        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<SalesReturnResponse>(decideResult.Error!);

        var lineIds = salesReturn.Lines.Select(l => l.Id.ToString()).ToList();
        var reverseStockResult = await reverseStockHandler.HandleAsync(new ReverseStockMovementsCommand("SalesReturnLine", lineIds), cancellationToken);
        if (!reverseStockResult.IsSuccess)
            return Result.Failure<SalesReturnResponse>(reverseStockResult.Error!);

        var reverseJournalsResult = await reverseGlJournalsHandler.HandleAsync(
            new ReverseGlJournalsCommand("SalesReturn", salesReturn.Id.ToString(), DateTimeOffset.UtcNow, $"Cancellation of {salesReturn.ReturnNo}"), cancellationToken);
        if (!reverseJournalsResult.IsSuccess)
            return Result.Failure<SalesReturnResponse>(reverseJournalsResult.Error!);

        // ReverseStockMovementsCommand runs its own retryable transaction and calls
        // ChangeTracker.Clear() at the start of every attempt — that detaches the `salesReturn` this
        // handler loaded earlier, so mutating it and calling SaveChangesAsync would silently persist
        // nothing. ExecuteUpdateAsync writes directly, independent of the tracker.
        var cancelledAt = DateTimeOffset.UtcNow;
        salesReturn.Status = "CANCELLED";
        salesReturn.CancelledBy = command.ApproverUserId;
        salesReturn.CancelledAt = cancelledAt;
        await dbContext.SalesReturns.Where(r => r.Id == salesReturn.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Status, "CANCELLED")
                .SetProperty(r => r.CancelledBy, command.ApproverUserId)
                .SetProperty(r => r.CancelledAt, cancelledAt), cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("SALES_RETURN", salesReturn.Id.ToString(), salesReturn.BranchId, "CANCELLATION_APPROVED", "ACTIVITY",
                "approved the cancellation request", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<SalesReturnResponse>(notifyResult.Error!);

        return Result.Success(SalesReturnMapper.ToResponse(salesReturn));
    }
}
