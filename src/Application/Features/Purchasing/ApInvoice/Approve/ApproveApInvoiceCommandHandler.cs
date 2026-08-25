namespace ZARI.Application.Features.Purchasing.ApInvoices.Approve;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
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
            .Include(i => i.GoodsReceiptPo)
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);

        if (invoice is null)
            return Result.Failure<ApInvoiceResponse>(Error.NotFound("ApInvoice.NotFound", $"AP invoice with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("AP_INVOICES", FormAction.Approve, invoice.BranchId, cancellationToken))
            return Result.Failure<ApInvoiceResponse>(Error.Forbidden("ApInvoice.Forbidden", "You do not have permission to approve AP invoices for this branch."));

        if (invoice.Status != "PENDING_APPROVAL")
            return Result.Failure<ApInvoiceResponse>(Error.Validation("ApInvoice.NotPendingApproval", "Only AP invoices pending approval can be approved."));

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

    private async Task<Result> PostApInvoiceJournalAsync(ApInvoice invoice, CancellationToken cancellationToken)
    {
        var total = invoice.Lines.Sum(l => Math.Round(l.Qty * l.UnitCost, 4));
        if (total <= 0)
            return Result.Success();

        var grniAccountResult = await GetDefaultAccountIdAsync("2100", "Goods Received Not Invoiced", cancellationToken);
        if (!grniAccountResult.IsSuccess)
            return Result.Failure(grniAccountResult.Error!);

        var apAccountResult = await GetDefaultAccountIdAsync("2000", "Accounts Payable", cancellationToken);
        if (!apAccountResult.IsSuccess)
            return Result.Failure(apAccountResult.Error!);

        var lines = new List<PostGlJournalLineInput>
        {
            new(grniAccountResult.Value, null, total, 0, null),
            new(apAccountResult.Value, null, 0, total, null)
        };

        var description = $"AP Invoice {invoice.InvoiceNo} — {invoice.Supplier.Name}";
        var postResult = await postGlJournalHandler.HandleAsync(
            new PostGlJournalCommand(invoice.BranchId, invoice.InvoiceDate, "PURCHASING", "ApInvoice", invoice.Id.ToString(), description, lines), cancellationToken);
        return postResult.IsSuccess ? Result.Success() : Result.Failure(postResult.Error!);
    }

    private async Task<Result<Guid>> GetDefaultAccountIdAsync(string code, string label, CancellationToken cancellationToken)
    {
        var accountId = await dbContext.GlAccounts.Where(a => a.Code == code).Select(a => (Guid?)a.Id).FirstOrDefaultAsync(cancellationToken);
        return accountId is null
            ? Result.Failure<Guid>(Error.NotFound("GlAccount.NotFound", $"Default GL account '{label}' ({code}) is not configured — check the seeded chart of accounts."))
            : Result.Success(accountId.Value);
    }
}
