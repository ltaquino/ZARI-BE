namespace ZARI.Application.Features.Purchasing.ApInvoices.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.ApInvoices.GetAll;
using ZARI.Application.Features.Purchasing.ApInvoices.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Two invoice shapes share this handler:
///  - ITEM: bills against a single, already-posted GRPO. The client is expected to have
///    pre-populated Lines by copying the GRPO's lines (see ApInvoiceFormPage), but this handler
///    trusts and validates whatever the client sends rather than re-deriving lines server-side,
///    same as every other module's Create.
///  - EXPENSE: bills a vendor directly with no GRPO — utilities, professional fees, manpower/
///    salaries, etc. Each line picks a GL expense/COGS account and a free-text description instead
///    of an item/qty/uom.
/// </summary>
public sealed class CreateApInvoiceCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreateApInvoiceCommand, Result<ApInvoiceResponse>>
{
    private static readonly string[] ExpenseAccountTypes = ["Expense", "Cogs"];

    public async Task<Result<ApInvoiceResponse>> HandleAsync(CreateApInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("AP_INVOICES", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<ApInvoiceResponse>(Error.Forbidden("ApInvoice.Forbidden", "You do not have permission to create AP invoices for this branch."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<ApInvoiceResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var supplier = await dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == command.SupplierId, cancellationToken);
        if (supplier is null)
            return Result.Failure<ApInvoiceResponse>(Error.NotFound("Supplier.NotFound", $"Supplier with ID '{command.SupplierId}' was not found."));

        var duplicateExists = await dbContext.ApInvoices.AnyAsync(
            i => i.SupplierId == command.SupplierId && i.SupplierInvoiceNo == command.SupplierInvoiceNo, cancellationToken);
        if (duplicateExists)
            return Result.Failure<ApInvoiceResponse>(Error.Conflict("ApInvoice.DuplicateSupplierInvoice", "This supplier invoice number has already been recorded for this supplier."));

        GoodsReceiptPo? grpo = null;
        Dictionary<Guid, Item> items = [];
        Dictionary<Guid, Uom> uoms = [];
        Dictionary<Guid, GlAccount> glAccounts = [];

        if (command.InvoiceType == "EXPENSE")
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
            grpo = await dbContext.GoodsReceiptPos
                .Include(g => g.Lines).ThenInclude(l => l.Item)
                .Include(g => g.Lines).ThenInclude(l => l.Uom)
                .FirstOrDefaultAsync(g => g.Id == command.GoodsReceiptPoId, cancellationToken);
            if (grpo is null)
                return Result.Failure<ApInvoiceResponse>(Error.NotFound("GoodsReceiptPo.NotFound", $"Goods receipt (PO) with ID '{command.GoodsReceiptPoId}' was not found."));

            if (grpo.Status != "POSTED")
                return Result.Failure<ApInvoiceResponse>(Error.Validation("ApInvoice.GrpoNotPosted", "The referenced goods receipt (PO) must be posted before it can be invoiced."));

            if (grpo.SupplierId != command.SupplierId)
                return Result.Failure<ApInvoiceResponse>(Error.Validation("ApInvoice.SupplierMismatch", "The invoice supplier must match the supplier on the referenced goods receipt (PO)."));

            var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
            items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
            if (items.Count != itemIds.Count)
                return Result.Failure<ApInvoiceResponse>(Error.NotFound("Item.NotFound", "One or more items on this invoice were not found."));

            var uomIds = command.Lines.Select(l => l.UomId).Distinct().ToList();
            uoms = await dbContext.Uoms.Where(u => uomIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);
            if (uoms.Count != uomIds.Count)
                return Result.Failure<ApInvoiceResponse>(Error.NotFound("Uom.NotFound", "One or more units of measure on this invoice were not found."));
        }

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "APINV"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<ApInvoiceResponse>(numberResult.Error!);

        var invoice = new ApInvoice
        {
            InvoiceNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            SupplierId = command.SupplierId,
            InvoiceType = command.InvoiceType,
            GoodsReceiptPoId = command.GoodsReceiptPoId,
            SupplierInvoiceNo = command.SupplierInvoiceNo,
            InvoiceDate = command.InvoiceDate,
            DueDate = command.DueDate,
            Status = "DRAFT",
            Remarks = command.Remarks,
            CreatedBy = command.CreatedBy,
            Lines = command.Lines.Select(l => new ApInvoiceLine
            {
                ItemId = l.ItemId,
                Qty = l.Qty,
                UomId = l.UomId,
                UnitCost = l.UnitCost
            }).ToList(),
            ExpenseLines = command.ExpenseLines.Select(l => new ApInvoiceExpenseLine
            {
                GlAccountId = l.GlAccountId,
                Description = l.Description,
                Amount = l.Amount
            }).ToList()
        };

        dbContext.ApInvoices.Add(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);

        invoice.Supplier = supplier;
        invoice.GoodsReceiptPo = grpo;
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
            new CreateNotificationCommand("AP_INVOICE", invoice.Id.ToString(), invoice.BranchId, "CREATED", "ACTIVITY",
                "created this AP invoice", command.CreatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<ApInvoiceResponse>(notifyResult.Error!);

        return Result.Success(ApInvoiceMapper.ToResponse(invoice));
    }
}
