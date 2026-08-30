namespace ZARI.Application.Features.Sales.SalesInvoices.RejectCancellation;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesInvoices.GetAll;
using ZARI.Application.Features.Sales.SalesInvoices.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>PENDING_CANCELLATION -> POSTED. The HQ admin declines the request; the document stands as posted.</summary>
public sealed class RejectSalesInvoiceCancellationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<RejectSalesInvoiceCancellationCommand, Result<SalesInvoiceResponse>>
{
    public async Task<Result<SalesInvoiceResponse>> HandleAsync(RejectSalesInvoiceCancellationCommand command, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.SalesInvoices
            .Include(i => i.Customer)
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines).ThenInclude(l => l.Uom)
            .Include(i => i.Lines).ThenInclude(l => l.StatutoryDiscountType)
            .FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);

        if (invoice is null)
            return Result.Failure<SalesInvoiceResponse>(Error.NotFound("SalesInvoice.NotFound", $"Sales invoice with ID '{command.Id}' was not found."));

        if (!await permissionService.HasCancellationAuthorityAsync("SALES_INVOICES", cancellationToken))
            return Result.Failure<SalesInvoiceResponse>(Error.Forbidden("SalesInvoice.Forbidden", "Only someone with cancel permission assigned to the head office branch can decide a cancellation request."));

        if (invoice.Status != "PENDING_CANCELLATION")
            return Result.Failure<SalesInvoiceResponse>(Error.Validation("SalesInvoice.NotPendingCancellation", "Only a sales invoice pending cancellation can have that request rejected."));

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "SALES_INVOICE" && r.EntityId == invoice.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<SalesInvoiceResponse>(Error.NotFound("ApprovalRequest.NotFound", "No cancellation request found for this sales invoice."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Reject", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<SalesInvoiceResponse>(decideResult.Error!);

        invoice.Status = "POSTED";
        invoice.CancelReason = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("SALES_INVOICE", invoice.Id.ToString(), invoice.BranchId, "CANCELLATION_REJECTED", "ACTIVITY",
                $"declined the cancellation request — \"{command.Comments}\"", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<SalesInvoiceResponse>(notifyResult.Error!);

        return Result.Success(SalesInvoiceMapper.ToResponse(invoice));
    }
}
