namespace ZARI.Application.Features.Inventory.StockTransferRequests.Decline;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockTransferRequests.GetAll;
using ZARI.Application.Features.Inventory.StockTransferRequests.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// APPROVED -> DECLINED. The fulfilling (source) branch's manager declines instead of ever
/// creating a Goods Issue against it. Terminal — Branch A would need to raise a new request. This
/// is a direct status change, not part of the ApprovalRequest workflow — the FE prototype never
/// touches an approval request here (unlike Approve/Reject).
/// </summary>
public sealed class DeclineStockTransferRequestCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler)
    : ICommandHandler<DeclineStockTransferRequestCommand, Result<StockTransferRequestResponse>>
{
    public async Task<Result<StockTransferRequestResponse>> HandleAsync(DeclineStockTransferRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.StockTransferRequests
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (request is null)
            return Result.Failure<StockTransferRequestResponse>(Error.NotFound("StockTransferRequest.NotFound", $"Stock transfer request with ID '{command.Id}' was not found."));

        if (request.Status != "APPROVED")
            return Result.Failure<StockTransferRequestResponse>(Error.Validation("StockTransferRequest.NotApproved", "Only an approved stock transfer request can be declined."));

        request.Status = "DECLINED";
        request.DeclinedBy = command.DeclinedBy;
        request.DeclinedAt = DateTimeOffset.UtcNow;
        request.DeclineReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("STOCK_TRANSFER_REQUEST", request.Id.ToString(), request.DestBranchId, "DECLINED", "ACTIVITY",
                $"declined this stock transfer request — \"{command.Reason}\"", command.DeclinedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<StockTransferRequestResponse>(notifyResult.Error!);

        return Result.Success(StockTransferRequestMapper.ToResponse(request));
    }
}
