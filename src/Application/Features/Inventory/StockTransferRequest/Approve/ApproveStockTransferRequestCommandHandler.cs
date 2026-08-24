namespace ZARI.Application.Features.Inventory.StockTransferRequests.Approve;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockTransferRequests.GetAll;
using ZARI.Application.Features.Inventory.StockTransferRequests.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// PENDING_APPROVAL -> APPROVED. Approved by the *requesting* branch's own manager — the
/// fulfilling (source) branch never approves; they're only notified once it's approved and may
/// optionally Decline it (see DeclineStockTransferRequestCommandHandler). No stock/GL side effects —
/// this document is pure workflow state; the eventual Goods Issue is what actually moves stock.
/// </summary>
public sealed class ApproveStockTransferRequestCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler)
    : ICommandHandler<ApproveStockTransferRequestCommand, Result<StockTransferRequestResponse>>
{
    public async Task<Result<StockTransferRequestResponse>> HandleAsync(ApproveStockTransferRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.StockTransferRequests
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (request is null)
            return Result.Failure<StockTransferRequestResponse>(Error.NotFound("StockTransferRequest.NotFound", $"Stock transfer request with ID '{command.Id}' was not found."));

        if (request.Status != "PENDING_APPROVAL")
            return Result.Failure<StockTransferRequestResponse>(Error.Validation("StockTransferRequest.NotPendingApproval", "Only stock transfer requests pending approval can be approved."));

        var approvalRequest = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "STOCK_TRANSFER_REQUEST" && r.EntityId == request.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (approvalRequest is null)
            return Result.Failure<StockTransferRequestResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this stock transfer request."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(approvalRequest.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<StockTransferRequestResponse>(decideResult.Error!);

        request.Status = "APPROVED";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("STOCK_TRANSFER_REQUEST", request.Id.ToString(), request.DestBranchId, "APPROVED", "ACTIVITY",
                "approved this stock transfer request", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<StockTransferRequestResponse>(notifyResult.Error!);

        return Result.Success(StockTransferRequestMapper.ToResponse(request));
    }
}
