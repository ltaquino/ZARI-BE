namespace ZARI.Application.Features.Sales.DeliveryOrders.RequestCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.DeliveryOrders.GetAll;
using ZARI.Application.Features.Sales.DeliveryOrders.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>POSTED -> PENDING_CANCELLATION. A same-branch manager flags it; only an HQ admin can finish the cancellation.</summary>
public sealed class RequestDeliveryOrderCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RequestDeliveryOrderCancellationCommand, Result<DeliveryOrderResponse>>
{
    public async Task<Result<DeliveryOrderResponse>> HandleAsync(RequestDeliveryOrderCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.DeliveryOrders
            .Include(d => d.Customer)
            .Include(d => d.Lines).ThenInclude(l => l.Item)
            .Include(d => d.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken);

        if (order is null)
            return Result.Failure<DeliveryOrderResponse>(Error.NotFound("DeliveryOrder.NotFound", $"Delivery with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("DELIVERIES", FormAction.Cancel, order.BranchId, cancellationToken))
            return Result.Failure<DeliveryOrderResponse>(Error.Forbidden("DeliveryOrder.Forbidden", "You do not have permission to request cancellation of deliveries for this branch."));

        if (order.Status != "POSTED")
            return Result.Failure<DeliveryOrderResponse>(Error.Validation("DeliveryOrder.NotPosted", "Only a posted delivery can have its cancellation requested."));

        var downstreamCheckResult = await CheckNoDownstreamPostedDocumentsAsync(dbContext, order.Lines.Select(l => l.Id).ToList(), cancellationToken);
        if (!downstreamCheckResult.IsSuccess)
            return Result.Failure<DeliveryOrderResponse>(downstreamCheckResult.Error!);

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("DELIVERY_ORDER", order.Id.ToString(), order.BranchId, command.RequestedBy, "CANCEL", command.Reason),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<DeliveryOrderResponse>(submitResult.Error!);

        order.Status = "PENDING_CANCELLATION";
        order.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("DELIVERY_ORDER", order.Id.ToString(), order.BranchId, "CANCELLATION_REQUESTED", "APPROVAL_NEEDED",
                $"requested cancellation — \"{command.Reason}\"", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<DeliveryOrderResponse>(notifyResult.Error!);

        return Result.Success(DeliveryOrderMapper.ToResponse(order));
    }

    /// <summary>
    /// Blocks cancelling a Delivery that's already been (partially or fully) invoiced or returned —
    /// reversing the original stock-out/COGS journal out from under a POSTED Sales Invoice or Sales
    /// Return would strand the wrong stock/GL picture. Sales Invoice/Sales Return CQRS don't exist
    /// yet as buildable modules (Waves 3/4), but their tables and DeliveryOrderLineId FKs already do
    /// (Wave 0), so this check is live and protective from day one — it just can't find a match
    /// until those modules ship. Checked here (friendly, at request time) and again in
    /// ApproveDeliveryOrderCancellationCommandHandler (authoritative, since an invoice/return could
    /// be approved in the gap between request and approval).
    /// </summary>
    internal static async Task<Result> CheckNoDownstreamPostedDocumentsAsync(IAppDbContext dbContext, List<Guid> lineIds, CancellationToken cancellationToken)
    {
        var hasPostedInvoice = await dbContext.SalesInvoiceLines
            .AnyAsync(l => l.DeliveryOrderLineId.HasValue && lineIds.Contains(l.DeliveryOrderLineId.Value) && l.SalesInvoice.Status == "POSTED", cancellationToken);
        if (hasPostedInvoice)
            return Result.Failure(Error.Validation("DeliveryOrder.HasPostedSalesInvoice", "This delivery can't be cancelled — a posted sales invoice already bills against it."));

        var hasPostedReturn = await dbContext.SalesReturnLines
            .AnyAsync(l => l.DeliveryOrderLineId.HasValue && lineIds.Contains(l.DeliveryOrderLineId.Value) && l.SalesReturn.Status == "POSTED", cancellationToken);
        if (hasPostedReturn)
            return Result.Failure(Error.Validation("DeliveryOrder.HasPostedSalesReturn", "This delivery can't be cancelled — a posted sales return already references it."));

        return Result.Success();
    }
}
