namespace ZARI.Application.Features.Inventory.StockOpnames.Cancel;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockOpnames.GetAll;
using ZARI.Application.Features.Inventory.StockOpnames.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// Direct cancel for DRAFT only — nothing's posted yet, so no reversal is needed. A POSTED count
/// has to go through RequestStockOpnameCancellation instead.
/// </summary>
public sealed class CancelStockOpnameCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CancelPendingApprovalRequestCommand, Result> cancelPendingHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler)
    : ICommandHandler<CancelStockOpnameCommand, Result<StockOpnameResponse>>
{
    public async Task<Result<StockOpnameResponse>> HandleAsync(CancelStockOpnameCommand command, CancellationToken cancellationToken = default)
    {
        var opname = await dbContext.StockOpnames
            .Include(o => o.Lines).ThenInclude(l => l.Item).ThenInclude(i => i.BaseUom)
            .FirstOrDefaultAsync(o => o.Id == command.Id, cancellationToken);

        if (opname is null)
            return Result.Failure<StockOpnameResponse>(Error.NotFound("StockOpname.NotFound", $"Stock opname with ID '{command.Id}' was not found."));

        if (opname.Status == "CANCELLED")
            return Result.Failure<StockOpnameResponse>(Error.Validation("StockOpname.AlreadyCancelled", "This stock count is already cancelled."));

        if (opname.Status is "POSTED" or "PENDING_CANCELLATION")
            return Result.Failure<StockOpnameResponse>(Error.Validation("StockOpname.RequiresCancellationRequest", "A posted stock count must go through a cancellation request instead."));

        var cancelPendingResult = await cancelPendingHandler.HandleAsync(new CancelPendingApprovalRequestCommand("STOCK_OPNAME", opname.Id.ToString()), cancellationToken);
        if (!cancelPendingResult.IsSuccess)
            return Result.Failure<StockOpnameResponse>(cancelPendingResult.Error!);

        opname.Status = "CANCELLED";
        opname.CancelledBy = command.CancelledBy;
        opname.CancelledAt = DateTimeOffset.UtcNow;
        opname.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("STOCK_OPNAME", opname.Id.ToString(), opname.BranchId, "CANCELLED", "ACTIVITY",
                $"cancelled this stock count — \"{command.Reason}\"", command.CancelledBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<StockOpnameResponse>(notifyResult.Error!);

        return Result.Success(StockOpnameMapper.ToResponse(opname));
    }
}
