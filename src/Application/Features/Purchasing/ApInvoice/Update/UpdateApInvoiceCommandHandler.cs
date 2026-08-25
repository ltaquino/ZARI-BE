namespace ZARI.Application.Features.Purchasing.ApInvoices.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.ApInvoices.GetAll;
using ZARI.Application.Features.Purchasing.ApInvoices.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// DRAFT-only edit. The referenced GRPO (and therefore the supplier) is immutable once set — only
/// line qty/cost, the vendor's own invoice number, dates, and remarks can change.
/// </summary>
public sealed class UpdateApInvoiceCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<UpdateApInvoiceCommand, Result<ApInvoiceResponse>>
{
    public async Task<Result<ApInvoiceResponse>> HandleAsync(UpdateApInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.ApInvoices
            .Include(i => i.Supplier)
            .Include(i => i.GoodsReceiptPo)
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);

        if (invoice is null)
            return Result.Failure<ApInvoiceResponse>(Error.NotFound("ApInvoice.NotFound", $"AP invoice with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("AP_INVOICES", FormAction.Edit, invoice.BranchId, cancellationToken))
            return Result.Failure<ApInvoiceResponse>(Error.Forbidden("ApInvoice.Forbidden", "You do not have permission to update AP invoices for this branch."));

        if (invoice.Status != "DRAFT")
            return Result.Failure<ApInvoiceResponse>(Error.Validation("ApInvoice.NotDraft", "Only draft AP invoices can be edited."));

        var duplicateExists = await dbContext.ApInvoices.AnyAsync(
            i => i.Id != command.Id && i.SupplierId == invoice.SupplierId && i.SupplierInvoiceNo == command.SupplierInvoiceNo, cancellationToken);
        if (duplicateExists)
            return Result.Failure<ApInvoiceResponse>(Error.Conflict("ApInvoice.DuplicateSupplierInvoice", "This supplier invoice number has already been recorded for this supplier."));

        var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        if (items.Count != itemIds.Count)
            return Result.Failure<ApInvoiceResponse>(Error.NotFound("Item.NotFound", "One or more items on this invoice were not found."));

        var uomIds = command.Lines.Select(l => l.UomId).Distinct().ToList();
        var uoms = await dbContext.Uoms.Where(u => uomIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);
        if (uoms.Count != uomIds.Count)
            return Result.Failure<ApInvoiceResponse>(Error.NotFound("Uom.NotFound", "One or more units of measure on this invoice were not found."));

        invoice.SupplierInvoiceNo = command.SupplierInvoiceNo;
        invoice.InvoiceDate = command.InvoiceDate;
        invoice.DueDate = command.DueDate;
        invoice.Remarks = command.Remarks;

        invoice.Lines.Clear();
        foreach (var line in command.Lines)
        {
            invoice.Lines.Add(new ApInvoiceLine
            {
                ItemId = line.ItemId,
                Qty = line.Qty,
                UomId = line.UomId,
                UnitCost = line.UnitCost
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var line in invoice.Lines)
        {
            line.Item = items[line.ItemId];
            line.Uom = uoms[line.UomId];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("AP_INVOICE", invoice.Id.ToString(), invoice.BranchId, "UPDATED", "ACTIVITY",
                "updated this AP invoice", command.UpdatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<ApInvoiceResponse>(notifyResult.Error!);

        return Result.Success(ApInvoiceMapper.ToResponse(invoice));
    }
}
