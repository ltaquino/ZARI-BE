namespace ZARI.Application.Features.Purchasing.ApInvoices.Approve;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Purchasing.ApInvoices.Create;
using ZARI.Application.Features.Purchasing.ApInvoices.GetAll;
using ZARI.Application.Features.Purchasing.ApInvoices.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// PENDING_APPROVAL -> POSTED. Unlike GRPO, an AP invoice never touches physical stock — no
/// ReceiveStockCommand/IssueStockLinesCommand calls at all — it only converts the "2100" GRNI
/// holding liability into a real "2000" Accounts Payable liability via a single balanced GL
/// journal. Since nothing here detaches the change tracker (no stock engine call runs its own
/// retryable transaction), a plain SaveChangesAsync is enough — no ExecuteUpdateAsync needed.
/// </summary>
public sealed class ApproveApInvoiceCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<ApproveApInvoiceCommand, Result<ApInvoiceResponse>>
{
    public async Task<Result<ApInvoiceResponse>> HandleAsync(ApproveApInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.ApInvoices
            .Include(i => i.Supplier)
            .Include(i => i.GoodsReceiptPo).ThenInclude(g => g!.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines).ThenInclude(l => l.Uom)
            .Include(i => i.ExpenseLines).ThenInclude(l => l.GlAccount)
            .FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);

        if (invoice is null)
            return Result.Failure<ApInvoiceResponse>(Error.NotFound("ApInvoice.NotFound", $"AP invoice with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("AP_INVOICES", FormAction.Approve, invoice.BranchId, cancellationToken))
            return Result.Failure<ApInvoiceResponse>(Error.Forbidden("ApInvoice.Forbidden", "You do not have permission to approve AP invoices for this branch."));

        if (invoice.Status != "PENDING_APPROVAL")
            return Result.Failure<ApInvoiceResponse>(Error.Validation("ApInvoice.NotPendingApproval", "Only AP invoices pending approval can be approved."));

        // Authoritative re-check, run BEFORE deciding the approval request — DecideApprovalRequestCommand
        // is an atomic, one-shot compare-and-swap with no way back (a second decide attempt on the same
        // request always fails with ApprovalRequest.AlreadyDecided). If this quantity check instead ran
        // after decide (as part of posting the journal, where it used to live), a second invoice racing
        // the same GRPO line could get its ApprovalRequest flipped to APPROVED and then fail the journal
        // step — leaving the document permanently stuck: approved-but-not-POSTED, with no code path left
        // to approve, reject, or cancel it. Same ordering ApproveGoodsReceiptPo/ApproveGoodsReturn/
        // ApprovePurchaseOrder already use.
        if (invoice.InvoiceType != "EXPENSE")
        {
            var quantityCheckResult = await ValidateItemInvoiceQuantitiesAsync(invoice, cancellationToken);
            if (!quantityCheckResult.IsSuccess)
                return Result.Failure<ApInvoiceResponse>(quantityCheckResult.Error!);
        }

        var request = await dbContext.ApprovalRequests
            .Where(r => r.EntityType == "AP_INVOICE" && r.EntityId == invoice.Id.ToString())
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null)
            return Result.Failure<ApInvoiceResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this AP invoice."));

        var decideResult = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
        if (!decideResult.IsSuccess)
            return Result.Failure<ApInvoiceResponse>(decideResult.Error!);

        var journalResult = await PostApInvoiceJournalAsync(invoice, cancellationToken);
        if (!journalResult.IsSuccess)
            return Result.Failure<ApInvoiceResponse>(journalResult.Error!);

        invoice.Status = "POSTED";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("AP_INVOICE", invoice.Id.ToString(), invoice.BranchId, "APPROVED", "ACTIVITY",
                "approved this AP invoice", command.ApproverUserId),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<ApInvoiceResponse>(notifyResult.Error!);

        return Result.Success(ApInvoiceMapper.ToResponse(invoice));
    }

    /// <summary>Dispatches to the ITEM (GRNI-clearing) or EXPENSE (direct-billing) journal shape.</summary>
    private Task<Result> PostApInvoiceJournalAsync(ApInvoice invoice, CancellationToken cancellationToken) =>
        invoice.InvoiceType == "EXPENSE"
            ? PostExpenseInvoiceJournalAsync(invoice, cancellationToken)
            : PostItemInvoiceJournalAsync(invoice, cancellationToken);

    /// <summary>
    /// No GRPO, no GRNI, no PPV — each expense line debits the GL account the encoder picked for it
    /// (utilities, professional fees, manpower/salaries, etc.), one journal line per invoice line so
    /// each keeps its own description as the line memo. Single balanced journal: Dr each expense
    /// account for its own line amount = Cr AP(total).
    /// </summary>
    private async Task<Result> PostExpenseInvoiceJournalAsync(ApInvoice invoice, CancellationToken cancellationToken)
    {
        var total = invoice.ExpenseLines.Sum(l => Math.Round(l.Amount, 4));
        if (total <= 0)
            return Result.Success();

        var apAccountResult = await ResolveApAccountIdAsync(invoice.Supplier, cancellationToken);
        if (!apAccountResult.IsSuccess)
            return Result.Failure(apAccountResult.Error!);

        var lines = invoice.ExpenseLines
            .Select(l => new PostGlJournalLineInput(l.GlAccountId, invoice.CostCenterId, Math.Round(l.Amount, 4), 0, l.Description))
            .ToList();
        lines.Add(new PostGlJournalLineInput(apAccountResult.Value, invoice.CostCenterId, 0, total, null));

        var description = $"AP Invoice {invoice.InvoiceNo} — {invoice.Supplier.Name} (expense)";
        var postResult = await postGlJournalHandler.HandleAsync(
            new PostGlJournalCommand(invoice.BranchId, invoice.InvoiceDate, "PURCHASING", "ApInvoice", invoice.Id.ToString(), description, lines), cancellationToken);
        return postResult.IsSuccess ? Result.Success() : Result.Failure(postResult.Error!);
    }

    /// <summary>
    /// GRNI clears at the GRPO's own originally-received value for just the lines/quantities THIS
    /// invoice covers — not the invoice's own price, and not the GRPO's full total. A GRPO can now be
    /// billed across multiple partial AP invoices (Phase 18 quantity-tracking), so debiting the whole
    /// GRPO total on every one of them would over-clear GRNI; scoping to this invoice's own referenced
    /// lines (at the GRPO line's unit cost, qty-weighted) keeps each invoice clearing exactly its own
    /// share. If the vendor's bill differs from what was actually received, the difference is swept
    /// into "5200 Purchase Price Variance" rather than left as a stray balance in the GRNI holding
    /// account. Unfavorable (invoiced more than received) debits PPV; favorable (invoiced less)
    /// credits it. Still a single balanced journal: Dr GRNI(receivedValue) [+ Dr PPV(variance) if
    /// unfavorable] = Cr AP(invoiceTotal) [+ Cr PPV(-variance) if favorable].
    /// </summary>
    private async Task<Result> PostItemInvoiceJournalAsync(ApInvoice invoice, CancellationToken cancellationToken)
    {
        var invoiceTotal = invoice.Lines.Sum(l => Math.Round(l.Qty * l.UnitCost, 4));
        if (invoiceTotal <= 0)
            return Result.Success();

        var grpoLinesById = invoice.GoodsReceiptPo!.Lines.ToDictionary(l => l.Id);
        var receivedValue = invoice.Lines.Sum(l => Math.Round(l.Qty * grpoLinesById[l.GoodsReceiptPoLineId!.Value].UnitCost, 4));
        var variance = invoiceTotal - receivedValue;

        var grniAccountResult = await GetDefaultAccountIdAsync("2100", "Goods Received Not Invoiced", cancellationToken);
        if (!grniAccountResult.IsSuccess)
            return Result.Failure(grniAccountResult.Error!);

        var apAccountResult = await ResolveApAccountIdAsync(invoice.Supplier, cancellationToken);
        if (!apAccountResult.IsSuccess)
            return Result.Failure(apAccountResult.Error!);

        var lines = new List<PostGlJournalLineInput>
        {
            new(grniAccountResult.Value, invoice.CostCenterId, receivedValue, 0, null),
            new(apAccountResult.Value, invoice.CostCenterId, 0, invoiceTotal, null)
        };

        if (variance != 0)
        {
            var ppvAccountResult = await GetDefaultAccountIdAsync("5200", "Purchase Price Variance", cancellationToken);
            if (!ppvAccountResult.IsSuccess)
                return Result.Failure(ppvAccountResult.Error!);

            lines.Add(variance > 0
                ? new PostGlJournalLineInput(ppvAccountResult.Value, invoice.CostCenterId, variance, 0, null)
                : new PostGlJournalLineInput(ppvAccountResult.Value, invoice.CostCenterId, 0, -variance, null));
        }

        var description = $"AP Invoice {invoice.InvoiceNo} — {invoice.Supplier.Name}";
        var postResult = await postGlJournalHandler.HandleAsync(
            new PostGlJournalCommand(invoice.BranchId, invoice.InvoiceDate, "PURCHASING", "ApInvoice", invoice.Id.ToString(), description, lines), cancellationToken);
        return postResult.IsSuccess ? Result.Success() : Result.Failure(postResult.Error!);
    }

    /// <summary>
    /// Authoritative re-check, closing the race a friendly Create/Update-time check can't: another
    /// AP invoice against the same GRPO line may have been approved in between. This invoice is
    /// still PENDING_APPROVAL (not POSTED) right now, so it's naturally excluded from its own
    /// "already invoiced" tally — same pattern as ApprovePurchaseOrderCommandHandler. Called from
    /// HandleAsync BEFORE the approval request is decided (see the comment there for why the
    /// ordering matters).
    /// </summary>
    private async Task<Result> ValidateItemInvoiceQuantitiesAsync(ApInvoice invoice, CancellationToken cancellationToken)
    {
        var referencedLineIds = invoice.Lines.Where(l => l.GoodsReceiptPoLineId.HasValue).Select(l => l.GoodsReceiptPoLineId!.Value).Distinct().ToList();
        var alreadyInvoiced = await dbContext.ApInvoiceLines
            .Where(l => l.GoodsReceiptPoLineId.HasValue && referencedLineIds.Contains(l.GoodsReceiptPoLineId.Value) && l.ApInvoice.Status == "POSTED")
            .GroupBy(l => l.GoodsReceiptPoLineId!.Value)
            .Select(g => new { GoodsReceiptPoLineId = g.Key, Qty = g.Sum(l => l.Qty) })
            .ToDictionaryAsync(x => x.GoodsReceiptPoLineId, x => x.Qty, cancellationToken);

        var lineInputs = invoice.Lines.Select(l => new ApInvoiceLineInput(l.ItemId, l.Qty, l.UomId, l.UnitCost, l.GoodsReceiptPoLineId)).ToList();
        return CreateApInvoiceCommandHandler.ValidateAgainstGoodsReceiptPo(invoice.GoodsReceiptPo!, lineInputs, alreadyInvoiced);
    }

    private async Task<Result<Guid>> GetDefaultAccountIdAsync(string code, string label, CancellationToken cancellationToken)
    {
        var accountId = await dbContext.GlAccounts.Where(a => a.Code == code).Select(a => (Guid?)a.Id).FirstOrDefaultAsync(cancellationToken);
        return accountId is null
            ? Result.Failure<Guid>(Error.NotFound("GlAccount.NotFound", $"Default GL account '{label}' ({code}) is not configured — check the seeded chart of accounts."))
            : Result.Success(accountId.Value);
    }

    /// Uses the supplier's own AP control-account override if one is configured, falling back to the
    /// default "2000 Accounts Payable". A supplier's ApAccountId can never point at a deleted GL
    /// account — DeleteGlAccountCommandHandler already blocks that — so no existence re-check here.
    private Task<Result<Guid>> ResolveApAccountIdAsync(Supplier supplier, CancellationToken cancellationToken) =>
        supplier.ApAccountId.HasValue
            ? Task.FromResult(Result.Success(supplier.ApAccountId.Value))
            : GetDefaultAccountIdAsync("2000", "Accounts Payable", cancellationToken);
}
