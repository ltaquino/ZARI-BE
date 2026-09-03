namespace ZARI.Application.Features.Purchasing.ApInvoices.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.ApInvoices.Create;
using ZARI.Application.Features.Purchasing.ApInvoices.GetAll;
using ZARI.Application.Features.Purchasing.ApInvoices.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// DRAFT-only edit. InvoiceType, GoodsReceiptPoId (and therefore the supplier) are immutable once
/// set. An ITEM invoice can only have its item Lines replaced; an EXPENSE invoice can only have its
/// ExpenseLines replaced — the client is expected to only send the list matching the invoice's own
/// type, but this handler rejects the other list outright rather than silently ignoring it.
/// </summary>
public sealed class UpdateApInvoiceCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<UpdateApInvoiceCommand, Result<ApInvoiceResponse>>
{
    private static readonly string[] ExpenseAccountTypes = ["Expense", "Cogs"];

    public async Task<Result<ApInvoiceResponse>> HandleAsync(UpdateApInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.ApInvoices
            .Include(i => i.Supplier)
            .Include(i => i.GoodsReceiptPo).ThenInclude(g => g!.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines)
            .Include(i => i.ExpenseLines)
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

        if (invoice.InvoiceType == "EXPENSE")
        {
            if (command.Lines.Count > 0)
                return Result.Failure<ApInvoiceResponse>(Error.Validation("ApInvoice.InvalidLinesForType", "An expense invoice cannot have item lines."));
            if (command.ExpenseLines.Count == 0)
                return Result.Failure<ApInvoiceResponse>(Error.Validation("ApInvoice.NoLines", "At least one expense line is required."));
        }
        else
        {
            if (command.ExpenseLines.Count > 0)
                return Result.Failure<ApInvoiceResponse>(Error.Validation("ApInvoice.InvalidLinesForType", "An item invoice cannot have expense lines."));
            if (command.Lines.Count == 0)
                return Result.Failure<ApInvoiceResponse>(Error.Validation("ApInvoice.NoLines", "At least one item line is required."));
        }

        Dictionary<Guid, Item> items = [];
        Dictionary<Guid, Uom> uoms = [];
        Dictionary<Guid, GlAccount> glAccounts = [];

        if (invoice.InvoiceType == "EXPENSE")
        {
            var glAccountIds = command.ExpenseLines.Select(l => l.GlAccountId).Distinct().ToList();
            glAccounts = await dbContext.GlAccounts.Where(a => glAccountIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, cancellationToken);
            if (glAccounts.Count != glAccountIds.Count)
                return Result.Failure<ApInvoiceResponse>(Error.NotFound("GlAccount.NotFound", "One or more GL accounts on this invoice were not found."));

            var invalidAccount = glAccounts.Values.FirstOrDefault(a => !ExpenseAccountTypes.Contains(a.AccountType));
            if (invalidAccount is not null)
                return Result.Failure<ApInvoiceResponse>(Error.Validation("ApInvoice.InvalidExpenseAccount", $"'{invalidAccount.Name}' is not an expense account — pick an Expense or Cogs account for each line."));
        }
        else
        {
            var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
            items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
            if (items.Count != itemIds.Count)
                return Result.Failure<ApInvoiceResponse>(Error.NotFound("Item.NotFound", "One or more items on this invoice were not found."));

            var uomIds = command.Lines.Select(l => l.UomId).Distinct().ToList();
            uoms = await dbContext.Uoms.Where(u => uomIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);
            if (uoms.Count != uomIds.Count)
                return Result.Failure<ApInvoiceResponse>(Error.NotFound("Uom.NotFound", "One or more units of measure on this invoice were not found."));

            // This invoice is DRAFT (checked above), so it's never itself among the "POSTED" AP
            // invoices being summed here — no self-exclusion needed, same reasoning as PurchaseOrder's
            // own Update-time re-check.
            var referencedLineIds = command.Lines.Where(l => l.GoodsReceiptPoLineId.HasValue).Select(l => l.GoodsReceiptPoLineId!.Value).Distinct().ToList();
            var alreadyInvoiced = await dbContext.ApInvoiceLines
                .Where(l => l.GoodsReceiptPoLineId.HasValue && referencedLineIds.Contains(l.GoodsReceiptPoLineId.Value) && l.ApInvoice.Status == "POSTED")
                .GroupBy(l => l.GoodsReceiptPoLineId!.Value)
                .Select(g => new { GoodsReceiptPoLineId = g.Key, Qty = g.Sum(l => l.Qty) })
                .ToDictionaryAsync(x => x.GoodsReceiptPoLineId, x => x.Qty, cancellationToken);

            var validationResult = CreateApInvoiceCommandHandler.ValidateAgainstGoodsReceiptPo(invoice.GoodsReceiptPo!, command.Lines, alreadyInvoiced);
            if (!validationResult.IsSuccess)
                return Result.Failure<ApInvoiceResponse>(validationResult.Error!);
        }

        if (command.CostCenterId.HasValue && !await dbContext.CostCenters.AnyAsync(c => c.Id == command.CostCenterId.Value, cancellationToken))
            return Result.Failure<ApInvoiceResponse>(Error.NotFound("CostCenter.NotFound", $"Cost center with ID '{command.CostCenterId}' was not found."));

        invoice.SupplierInvoiceNo = command.SupplierInvoiceNo;
        invoice.InvoiceDate = command.InvoiceDate;
        invoice.DueDate = command.DueDate;
        invoice.Remarks = command.Remarks;
        invoice.CostCenterId = command.CostCenterId;

        invoice.Lines.Clear();
        foreach (var line in command.Lines)
        {
            invoice.Lines.Add(new ApInvoiceLine
            {
                ItemId = line.ItemId,
                Qty = line.Qty,
                UomId = line.UomId,
                UnitCost = line.UnitCost,
                GoodsReceiptPoLineId = line.GoodsReceiptPoLineId,
                VatType = line.VatType ?? items[line.ItemId].VatType
            });
        }

        invoice.ExpenseLines.Clear();
        foreach (var line in command.ExpenseLines)
        {
            invoice.ExpenseLines.Add(new ApInvoiceExpenseLine
            {
                GlAccountId = line.GlAccountId,
                Description = line.Description,
                Amount = line.Amount,
                VatType = line.VatType ?? "VATABLE"
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var line in invoice.Lines)
        {
            line.Item = items[line.ItemId];
            line.Uom = uoms[line.UomId];
        }
        foreach (var line in invoice.ExpenseLines)
        {
            line.GlAccount = glAccounts[line.GlAccountId];
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
