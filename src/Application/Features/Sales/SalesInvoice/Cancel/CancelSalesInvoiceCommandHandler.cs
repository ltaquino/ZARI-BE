namespace ZARI.Application.Features.Sales.SalesInvoices.Cancel;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesInvoices.GetAll;
using ZARI.Application.Features.Sales.SalesInvoices.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>
/// Direct cancel for DRAFT / PENDING_APPROVAL only — nothing's posted yet, so no GL reversal is
/// needed. A POSTED sales invoice has to go through RequestSalesInvoiceCancellation instead.
/// </summary>
public sealed class CancelSalesInvoiceCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CancelPendingApprovalRequestCommand, Result> cancelPendingHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CancelSalesInvoiceCommand, Result<SalesInvoiceResponse>>
{
    public async Task<Result<SalesInvoiceResponse>> HandleAsync(CancelSalesInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.SalesInvoices
            .Include(i => i.Customer)
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines).ThenInclude(l => l.Uom)
            .Include(i => i.Lines).ThenInclude(l => l.StatutoryDiscountType)
            .FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);

        if (invoice is null)
            return Result.Failure<SalesInvoiceResponse>(Error.NotFound("SalesInvoice.NotFound", $"Sales invoice with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("SALES_INVOICES", FormAction.Cancel, invoice.BranchId, cancellationToken))
            return Result.Failure<SalesInvoiceResponse>(Error.Forbidden("SalesInvoice.Forbidden", "You do not have permission to cancel sales invoices for this branch."));

        if (invoice.Status == "CANCELLED")
            return Result.Failure<SalesInvoiceResponse>(Error.Validation("SalesInvoice.AlreadyCancelled", "This sales invoice is already cancelled."));

        if (invoice.Status is not ("DRAFT" or "PENDING_APPROVAL"))
            return Result.Failure<SalesInvoiceResponse>(Error.Validation("SalesInvoice.RequiresCancellationRequest", "A posted sales invoice must go through a cancellation request instead."));

        var cancelPendingResult = await cancelPendingHandler.HandleAsync(new CancelPendingApprovalRequestCommand("SALES_INVOICE", invoice.Id.ToString()), cancellationToken);
        if (!cancelPendingResult.IsSuccess)
            return Result.Failure<SalesInvoiceResponse>(cancelPendingResult.Error!);

        invoice.Status = "CANCELLED";
        invoice.CancelledBy = command.CancelledBy;
        invoice.CancelledAt = DateTimeOffset.UtcNow;
        invoice.CancelReason = command.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("SALES_INVOICE", invoice.Id.ToString(), invoice.BranchId, "CANCELLED", "ACTIVITY",
                $"cancelled this sales invoice — \"{command.Reason}\"", command.CancelledBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<SalesInvoiceResponse>(notifyResult.Error!);

        return Result.Success(SalesInvoiceMapper.ToResponse(invoice));
    }
}
