namespace ZARI.Application.Features.Sales.PosSale;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Inventory.SerialNumbers.Issue;
using ZARI.Application.Features.Inventory.StockLedgers.Issue;
using ZARI.Application.Features.Sales.CustomerPayments.Create;
using ZARI.Application.Features.Sales.CustomerPayments.GetAll;
using ZARI.Application.Features.Sales.SalesInvoices.Create;
using ZARI.Application.Features.Sales.SalesInvoices.GetAll;
using ZARI.Application.Features.Sales.SalesInvoices.Shared;
using ZARI.Domain.Common;

/// <summary>
/// The POS checkout action: creates and force-quick-posts a Sales Invoice, issues stock + posts
/// COGS for it (PosStockPostingService — POS has no Delivery Order of its own to do that), then —
/// only if the invoice actually posted — creates and force-quick-posts the Customer Payment that
/// fully settles it. Composes existing Create handlers exactly the way an Approve handler composes
/// its own sub-steps elsewhere in this codebase (decide/post/notify), rather than re-implementing
/// invoice or payment posting here. If the invoice doesn't post (a discretionary discount exceeded
/// Company.MaxUnapprovedDiscountPct), nothing else happens — no stock movement, no payment, no
/// stuck partial state — and the cashier is told this sale needs a manager's approval through the
/// regular workflow instead of an instant POS checkout. (If stock/COGS posting itself fails after
/// the invoice already posted — e.g. a misconfigured GL account — the invoice is left POSTED with
/// no payment applied and no stock moved: the same kind of recoverable, not-corrupted partial state
/// already accepted below for a failed payment step, fixable by hand through the admin screens
/// rather than a fully atomic three-way transaction across independently-committed handlers.)
/// With a ClientReferenceId (ZARI Mobile always sends one) that partial state also heals itself: resending the same
/// checkout finds the invoice the first attempt created and completes only what's missing — stock/COGS (each step
/// skips what's already posted) and the payment (skipped once a posted payment covers the invoice) — so a lost
/// answer or a failure part-way is simply retried, and the sale is never booked twice.
/// </summary>
public sealed class CreatePosSaleCommandHandler(
    IAppDbContext dbContext,
    IPermissionService permissionService,
    ICommandHandler<CreateSalesInvoiceCommand, Result<SalesInvoiceResponse>> createSalesInvoiceHandler,
    ICommandHandler<IssueStockLinesCommand, Result<IssueStockLinesResponse>> issueStockLinesHandler,
    ICommandHandler<IssueSerialCommand, Result> issueSerialHandler,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<CreateCustomerPaymentCommand, Result<CustomerPaymentResponse>> createCustomerPaymentHandler)
    : ICommandHandler<CreatePosSaleCommand, Result<PosSaleResponse>>
{
    private const string WalkInCustomerName = "Walk-in Customer";

    public async Task<Result<PosSaleResponse>> HandleAsync(CreatePosSaleCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("POS_MODE", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<PosSaleResponse>(Error.Forbidden("PosSale.Forbidden", "You do not have permission to use POS Mode for this branch."));

        var terminal = await dbContext.PosTerminals.FirstOrDefaultAsync(t => t.Id == command.PosTerminalId && t.BranchId == command.BranchId, cancellationToken);
        if (terminal is null)
            return Result.Failure<PosSaleResponse>(Error.NotFound("PosSale.TerminalNotFound", "The selected POS terminal was not found at this branch."));
        if (terminal.Status != "active")
            return Result.Failure<PosSaleResponse>(Error.Validation("PosSale.TerminalInactive", "This POS terminal is not active."));
        if (await dbContext.Vehicles.AnyAsync(v => v.PosTerminalId == terminal.Id, cancellationToken))
            return Result.Failure<PosSaleResponse>(Error.Validation("PosSale.TruckTerminal",
                "That terminal belongs to a delivery truck — its invoices can only be issued from the truck's handheld."));

        var resumed = command.ClientReferenceId is { } clientReferenceId
            ? await dbContext.SalesInvoices.AsNoTracking().Include(i => i.Lines).ThenInclude(l => l.StatutoryDiscountType)
                .FirstOrDefaultAsync(i => i.ClientReferenceId == clientReferenceId, cancellationToken)
            : null;
        if (resumed is not null && resumed.PosTerminalId != command.PosTerminalId)
            return Result.Failure<PosSaleResponse>(Error.Conflict("PosSale.ClientReferenceReused", "That checkout id was already used for a sale on another terminal."));

        // POS is branch-scoped, not just branch-defaulted: an item can only be sold at a branch it
        // has an active ItemBranchSetting for (that's also where its POS price comes from) — a bare
        // Item row existing globally isn't enough. The regular Sales Invoice form stays as
        // permissive as before (a Guid ItemId with no branch setting still resolves to a 0 price
        // there, which staff notice and can override); POS's own scan flow already filters its
        // search results to only branch-configured items, so this is the same rule enforced
        // server-side rather than a new one — closing the gap where a client could still bypass
        // that FE filtering by calling this endpoint directly.
        var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        if (items.Count != itemIds.Count)
            return Result.Failure<PosSaleResponse>(Error.NotFound("Item.NotFound", "One or more items on this sale were not found."));

        var settingByItemId = (await dbContext.ItemBranchSettings
                .Where(s => s.BranchId == command.BranchId && s.Status == "active" && itemIds.Contains(s.ItemId))
                .ToListAsync(cancellationToken))
            .ToDictionary(s => s.ItemId);

        var missingSettingItemIds = itemIds.Where(id => !settingByItemId.ContainsKey(id)).ToList();
        if (missingSettingItemIds.Count > 0)
        {
            var missingCodes = missingSettingItemIds.Select(id => items[id].Code);
            return Result.Failure<PosSaleResponse>(Error.Validation("PosSale.ItemNotAvailableAtBranch",
                $"These items aren't set up for sale at this branch: {string.Join(", ", missingCodes)}. Add an Item Branch Setting for them first."));
        }

        // Which warehouse each stocked line issues from at checkout (PosStockPostingService) — a
        // non-stocked item (service) needs none. Resolved from the same ItemBranchSetting row
        // above, not a per-line input: POS has no concept of the cashier picking a warehouse.
        var missingWarehouseItemIds = itemIds.Where(id => items[id].IsStocked && settingByItemId[id].DefaultWarehouseId is null).ToList();
        if (missingWarehouseItemIds.Count > 0)
        {
            var missingCodes = missingWarehouseItemIds.Select(id => items[id].Code);
            return Result.Failure<PosSaleResponse>(Error.Validation("PosSale.ItemMissingDefaultWarehouse",
                $"These items need a default warehouse set on their Item Branch Setting before they can be sold at POS: {string.Join(", ", missingCodes)}."));
        }

        var warehouseIdByItemId = itemIds
            .Where(id => items[id].IsStocked)
            .ToDictionary(id => id, id => settingByItemId[id].DefaultWarehouseId!.Value);

        // A serialized item needs its specific unit identified before checkout, not just its
        // quantity — PosStockPostingService marks that exact serial SOLD. Fail upfront (before
        // anything is created) rather than discovering a missing/wrong/already-sold serial mid-flow.
        var serializedLines = command.Lines.Where(l => items[l.ItemId].IsSerialized).ToList();
        var missingSerialCodes = serializedLines.Where(l => string.IsNullOrWhiteSpace(l.SerialNo)).Select(l => items[l.ItemId].Code).Distinct().ToList();
        if (missingSerialCodes.Count > 0)
        {
            return Result.Failure<PosSaleResponse>(Error.Validation("PosSale.SerialNoRequired",
                $"A serial number is required for these serialized items: {string.Join(", ", missingSerialCodes)}."));
        }

        var duplicateSerialDescriptions = serializedLines
            .GroupBy(l => (l.ItemId, l.SerialNo))
            .Where(g => g.Count() > 1)
            .Select(g => $"{items[g.Key.ItemId].Code} #{g.Key.SerialNo}")
            .ToList();
        if (duplicateSerialDescriptions.Count > 0)
        {
            return Result.Failure<PosSaleResponse>(Error.Validation("PosSale.DuplicateSerialNo",
                $"The same serial number was scanned more than once: {string.Join(", ", duplicateSerialDescriptions)}."));
        }

        // (Skipped on a resend: the first attempt may already have marked these units SOLD.)
        if (serializedLines.Count > 0 && resumed is null)
        {
            var serializedItemIds = serializedLines.Select(l => l.ItemId).Distinct().ToList();
            var statusBySerialKey = (await dbContext.SerialNumbers
                    .Where(s => serializedItemIds.Contains(s.ItemId))
                    .Select(s => new { s.ItemId, s.SerialNo, s.Status })
                    .ToListAsync(cancellationToken))
                .ToDictionary(s => (s.ItemId, s.SerialNo), s => s.Status);

            var unavailable = serializedLines
                .Where(l => !statusBySerialKey.TryGetValue((l.ItemId, l.SerialNo!), out var status) || status != "IN_STOCK")
                .Select(l => $"{items[l.ItemId].Code} #{l.SerialNo}")
                .ToList();
            if (unavailable.Count > 0)
            {
                return Result.Failure<PosSaleResponse>(Error.Validation("PosSale.SerialNotAvailable",
                    $"These serial numbers are not currently in stock: {string.Join(", ", unavailable)}."));
            }
        }

        var customerId = command.CustomerId;
        if (!customerId.HasValue)
        {
            var walkIn = await dbContext.Customers.FirstOrDefaultAsync(c => c.BranchId == command.BranchId && c.Name == WalkInCustomerName, cancellationToken);
            if (walkIn is null)
                return Result.Failure<PosSaleResponse>(Error.NotFound("PosSale.WalkInCustomerNotFound", "No Walk-in Customer is configured for this branch — pick a member or contact an administrator."));
            customerId = walkIn.Id;
        }

        // Resolve which of the given tenders is CASH (if any) so an overtendered cash amount can be
        // capped down to what's actually kept as AR settlement — the excess is change handed back,
        // never itself part of the recorded payment. Card/Gift Check/etc. tenders are never adjusted.
        var methodIds = command.Tenders.Select(t => t.PaymentMethodId).Distinct().ToList();
        var methods = await dbContext.PaymentMethods.Where(m => methodIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id, cancellationToken);
        if (methods.Count != methodIds.Count)
            return Result.Failure<PosSaleResponse>(Error.NotFound("PosSale.PaymentMethodNotFound", "One or more payment methods were not found."));

        Guid invoiceId;
        string invoiceNo, invoiceStatus;
        string? birOrSeriesNumber;
        decimal invoiceTotal;
        if (resumed is null)
        {
            var invoiceResult = await createSalesInvoiceHandler.HandleAsync(new CreateSalesInvoiceCommand(
                command.BranchId, customerId.Value, null, command.InvoiceDate, null, "POS sale", command.DiscountPct, command.CostCenterId,
                command.CreatedBy, command.Lines, ForceQuickPost: true, PosTerminalId: command.PosTerminalId,
                ClientReferenceId: command.ClientReferenceId), cancellationToken);
            if (!invoiceResult.IsSuccess)
                return Result.Failure<PosSaleResponse>(invoiceResult.Error!);
            var invoice = invoiceResult.Value!;
            (invoiceId, invoiceNo, invoiceStatus, birOrSeriesNumber) = (invoice.Id, invoice.InvoiceNo, invoice.Status, invoice.BirOrSeriesNumber);
            invoiceTotal = invoice.Balance; // amountPaid is 0 immediately after creation, so Balance == the full total.
        }
        else
        {
            (invoiceId, invoiceNo, invoiceStatus, birOrSeriesNumber) = (resumed.Id, resumed.InvoiceNo, resumed.Status, resumed.BirOrSeriesNumber);
            invoiceTotal = SalesInvoicePaymentBalance.GetInvoiceTotal(resumed);
        }

        // POSTED on creation; PARTIALLY_PAID/PAID once a resumed sale's payment had already gone through.
        if (invoiceStatus is not ("POSTED" or "PARTIALLY_PAID" or "PAID"))
        {
            return Result.Failure<PosSaleResponse>(Error.Validation("PosSale.RequiresApproval",
                $"This sale's discount requires manager approval and can't be completed as an instant POS checkout — invoice {invoiceNo} was saved as {invoiceStatus} instead. Use the regular Sales Invoice workflow to submit it for approval."));
        }

        var stockResult = await PosStockPostingService.PostStockAndCogsAsync(
            dbContext, issueStockLinesHandler, issueSerialHandler, postGlJournalHandler, invoiceId, warehouseIdByItemId, cancellationToken);
        if (!stockResult.IsSuccess)
            return Result.Failure<PosSaleResponse>(stockResult.Error!);

        var tenderTotal = command.Tenders.Sum(t => t.Amount);
        var changeDue = Math.Max(0, Math.Round(tenderTotal - invoiceTotal, 4));

        var adjustedTenders = command.Tenders;
        if (changeDue > 0)
        {
            var cashTenderIndex = command.Tenders.FindIndex(t => methods[t.PaymentMethodId].Code == "CASH" && t.Amount >= changeDue);
            if (cashTenderIndex < 0)
                return Result.Failure<PosSaleResponse>(Error.Validation("PosSale.InvalidChange", "The amount tendered exceeds the total due, but no single cash tender covers the change owed — adjust the tender amounts."));

            adjustedTenders = [.. command.Tenders];
            adjustedTenders[cashTenderIndex] = adjustedTenders[cashTenderIndex] with { Amount = Math.Round(adjustedTenders[cashTenderIndex].Amount - changeDue, 4) };
        }

        // A resumed sale whose payment already went through: hand back that payment rather than paying twice.
        var paid = resumed is null ? null : await dbContext.CustomerPaymentLines.AsNoTracking()
            .Where(l => l.SalesInvoiceId == invoiceId && l.CustomerPayment.Status == "POSTED")
            .Select(l => new { l.CustomerPaymentId, l.CustomerPayment.PaymentNo })
            .FirstOrDefaultAsync(cancellationToken);
        if (paid is not null)
            return Result.Success(new PosSaleResponse(invoiceId, invoiceNo, birOrSeriesNumber, invoiceTotal, paid.CustomerPaymentId, paid.PaymentNo, changeDue));

        var paymentResult = await createCustomerPaymentHandler.HandleAsync(new CreateCustomerPaymentCommand(
            command.BranchId, customerId.Value, command.InvoiceDate, "POS sale payment", command.CostCenterId, command.CreatedBy,
            Lines: [new CustomerPaymentLineInput(invoiceId, invoiceTotal)],
            Tenders: adjustedTenders,
            ForceQuickPost: true), cancellationToken);
        if (!paymentResult.IsSuccess)
            return Result.Failure<PosSaleResponse>(paymentResult.Error!);

        var payment = paymentResult.Value!;
        return Result.Success(new PosSaleResponse(invoiceId, invoiceNo, birOrSeriesNumber, invoiceTotal, payment.Id, payment.PaymentNo, changeDue));
    }
}
