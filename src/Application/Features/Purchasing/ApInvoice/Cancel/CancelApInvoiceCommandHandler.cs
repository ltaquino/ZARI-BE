namespace ZARI.Application.Features.Purchasing.ApInvoices.Cancel;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.ApInvoices.GetAll;
using ZARI.Application.Features.Purchasing.ApInvoices.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// Direct cancel for DRAFT / PENDING_APPROVAL only — nothing's posted yet, so no reversal is
/// needed. A POSTED invoice has to go through RequestApInvoiceCancellation instead.
/// </summary>
public sealed class CancelApInvoiceCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CancelPendingApprovalRequestCommand, Result> cancelPendingHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CancelApInvoiceCommand, Result<ApInvoiceResponse>>
{
    public async Task<Result<ApInvoiceResponse>> HandleAsync(CancelApInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.ApInvoices
            .Include(i => i.Supplier)
            .Include(i => i.GoodsReceiptPo)
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines).ThenInclude(l => l.Uom)
            .Include(i => i.ExpenseLines).ThenInclude(l => l.GlAccount)
            .FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);

        if (invoice is null)
            return Result.Failure<ApInvoiceResponse>(Error.NotFound("ApInvoice.NotFound", $"AP invoice with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("AP_INVOICES", FormAction.Cancel, invoice.BranchId, cancellationToken))
            return Result.Failure<ApInvoiceResponse>(Error.Forbidden("ApInvoice.Forbidden", "You do not have permission to cancel AP invoices for this branch."));

        if (invoice.Status == "CANCELLED")
            return Result.Failure<ApInvoiceResponse>(Error.Validation("ApInvoice.AlreadyCancelled", "This AP invoice is already cancelled."));

        if (invoice.Status is "POSTED" or "PENDING_CANCELLATION")
            return Result.Failure<ApInvoiceResponse>(Error.Validation("ApInvoice.RequiresCancellationRequest", "A posted AP invoice must go through a cancellation request instead."));

        var cancelPendingResult = await cancelPendingHandler.HandleAsync(new CancelPendingApprovalRequestCommand("AP_INVOICE", invoice.Id.ToString()), cancellationToken);
        if (!cancelPendingResult.IsSuccess)
            return Result.Failure<ApInvoiceResponse>(cancelPendingResult.Error!);

        invoice.Status = "CANCELLED";
        invoice.CancelledBy = command.CancelledBy;
        invoice.CancelledAt = DateTimeOffset.UtcNow;
        invoice.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("AP_INVOICE", invoice.Id.ToString(), invoice.BranchId, "CANCELLED", "ACTIVITY",
                $"cancelled this AP invoice — \"{command.Reason}\"", command.CancelledBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<ApInvoiceResponse>(notifyResult.Error!);

        return Result.Success(ApInvoiceMapper.ToResponse(invoice));
    }
}
