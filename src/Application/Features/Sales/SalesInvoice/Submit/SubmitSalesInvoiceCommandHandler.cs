namespace ZARI.Application.Features.Sales.SalesInvoices.Submit;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesInvoices.GetAll;
using ZARI.Application.Features.Sales.SalesInvoices.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>DRAFT -> PENDING_APPROVAL. Creates the ApprovalRequest a checker will act on.</summary>
public sealed class SubmitSalesInvoiceCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<SubmitSalesInvoiceCommand, Result<SalesInvoiceResponse>>
{
    public async Task<Result<SalesInvoiceResponse>> HandleAsync(SubmitSalesInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.SalesInvoices
            .Include(i => i.Customer)
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines).ThenInclude(l => l.Uom)
            .Include(i => i.Lines).ThenInclude(l => l.StatutoryDiscountType)
            .FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);

        if (invoice is null)
            return Result.Failure<SalesInvoiceResponse>(Error.NotFound("SalesInvoice.NotFound", $"Sales invoice with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("SALES_INVOICES", FormAction.Edit, invoice.BranchId, cancellationToken))
            return Result.Failure<SalesInvoiceResponse>(Error.Forbidden("SalesInvoice.Forbidden", "You do not have permission to submit sales invoices for this branch."));

        if (invoice.Status != "DRAFT")
            return Result.Failure<SalesInvoiceResponse>(Error.Validation("SalesInvoice.NotDraft", "Only draft sales invoices can be submitted for approval."));

        if (invoice.Lines.Count == 0)
            return Result.Failure<SalesInvoiceResponse>(Error.Validation("SalesInvoice.NoLines", "Add at least one line before submitting for approval."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("SALES_INVOICE", invoice.Id.ToString(), invoice.BranchId, command.RequestedBy, null, null),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<SalesInvoiceResponse>(submitResult.Error!);

        invoice.Status = "PENDING_APPROVAL";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("SALES_INVOICE", invoice.Id.ToString(), invoice.BranchId, "SUBMITTED", "APPROVAL_NEEDED",
                "submitted this sales invoice for approval", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<SalesInvoiceResponse>(notifyResult.Error!);

        return Result.Success(SalesInvoiceMapper.ToResponse(invoice));
    }
}
