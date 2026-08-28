namespace ZARI.Application.Features.Purchasing.ApInvoices.Submit;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.ApInvoices.GetAll;
using ZARI.Application.Features.Purchasing.ApInvoices.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>DRAFT -> PENDING_APPROVAL. Creates the ApprovalRequest a checker will act on.</summary>
public sealed class SubmitApInvoiceCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<SubmitApInvoiceCommand, Result<ApInvoiceResponse>>
{
    public async Task<Result<ApInvoiceResponse>> HandleAsync(SubmitApInvoiceCommand command, CancellationToken cancellationToken = default)
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

        if (!await permissionService.HasPermissionOnBranchAsync("AP_INVOICES", FormAction.Edit, invoice.BranchId, cancellationToken))
            return Result.Failure<ApInvoiceResponse>(Error.Forbidden("ApInvoice.Forbidden", "You do not have permission to submit AP invoices for this branch."));

        if (invoice.Status != "DRAFT")
            return Result.Failure<ApInvoiceResponse>(Error.Validation("ApInvoice.NotDraft", "Only draft AP invoices can be submitted for approval."));

        if (invoice.Lines.Count == 0 && invoice.ExpenseLines.Count == 0)
            return Result.Failure<ApInvoiceResponse>(Error.Validation("ApInvoice.NoLines", "Add at least one line before submitting for approval."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("AP_INVOICE", invoice.Id.ToString(), invoice.BranchId, command.RequestedBy, null, null),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<ApInvoiceResponse>(submitResult.Error!);

        invoice.Status = "PENDING_APPROVAL";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("AP_INVOICE", invoice.Id.ToString(), invoice.BranchId, "SUBMITTED", "APPROVAL_NEEDED",
                "submitted this AP invoice for approval", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<ApInvoiceResponse>(notifyResult.Error!);

        return Result.Success(ApInvoiceMapper.ToResponse(invoice));
    }
}
