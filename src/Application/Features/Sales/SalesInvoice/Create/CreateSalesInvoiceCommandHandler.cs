namespace ZARI.Application.Features.Sales.SalesInvoices.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Sales.SalesInvoices.GetAll;
using ZARI.Application.Features.Sales.SalesInvoices.Shared;
using ZARI.Application.Features.Sales.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// The module's highest-stakes handler — the actual BIR-facing receipt. Mirrors
/// CreateGoodsReceiptPoCommandHandler's optional-upstream-document shape (here, an optional
/// DeliveryOrder) and CreateSalesOrderCommandHandler's quick-post/threshold mechanic, but adds the
/// real posting side effect on the quick-post path: when Company.SalesInvoiceQuickPostEnabled is on
/// and no discretionary discount breaches Company.MaxUnapprovedDiscountPct (statutory-discount
/// lines are excluded from that check — see DiscountThresholdPolicy usage below), this assigns the
/// BIR-OR number and posts the AR/Revenue/VAT journal right here via SalesInvoicePostingService —
/// the same engine ApproveSalesInvoiceCommandHandler calls.
/// </summary>
public sealed class CreateSalesInvoiceCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreateSalesInvoiceCommand, Result<SalesInvoiceResponse>>
{
    public async Task<Result<SalesInvoiceResponse>> HandleAsync(CreateSalesInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("SALES_INVOICES", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<SalesInvoiceResponse>(Error.Forbidden("SalesInvoice.Forbidden", "You do not have permission to create sales invoices for this branch."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<SalesInvoiceResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var customer = await dbContext.Customers.FirstOrDefaultAsync(c => c.Id == command.CustomerId, cancellationToken);
        if (customer is null)
            return Result.Failure<SalesInvoiceResponse>(Error.NotFound("Customer.NotFound", $"Customer with ID '{command.CustomerId}' was not found."));

        DeliveryOrder? deliveryOrder = null;
        if (command.DeliveryOrderId.HasValue)
        {
            deliveryOrder = await dbContext.DeliveryOrders
                .Include(d => d.Lines).ThenInclude(l => l.Item)
                .FirstOrDefaultAsync(d => d.Id == command.DeliveryOrderId.Value, cancellationToken);
            if (deliveryOrder is null)
                return Result.Failure<SalesInvoiceResponse>(Error.NotFound("DeliveryOrder.NotFound", $"Delivery with ID '{command.DeliveryOrderId}' was not found."));

            if (deliveryOrder.Status != "POSTED")
                return Result.Failure<SalesInvoiceResponse>(Error.Validation("SalesInvoice.DeliveryOrderNotPosted", "The referenced delivery must be approved before it can be invoiced against."));
        }
        else if (command.Lines.Any(l => l.DeliveryOrderLineId.HasValue))
        {
            return Result.Failure<SalesInvoiceResponse>(Error.Validation("SalesInvoice.UnexpectedDeliveryOrderLine", "Lines cannot reference a delivery line unless this invoice itself references a delivery."));
        }

        if (deliveryOrder is not null)
        {
            var referencedLineIds = command.Lines.Where(l => l.DeliveryOrderLineId.HasValue).Select(l => l.DeliveryOrderLineId!.Value).Distinct().ToList();
            var alreadyInvoiced = await dbContext.SalesInvoiceLines
                .Where(l => l.DeliveryOrderLineId.HasValue && referencedLineIds.Contains(l.DeliveryOrderLineId.Value) && l.SalesInvoice.Status == "POSTED")
                .GroupBy(l => l.DeliveryOrderLineId!.Value)
                .Select(g => new { DeliveryOrderLineId = g.Key, Qty = g.Sum(l => l.Qty) })
                .ToDictionaryAsync(x => x.DeliveryOrderLineId, x => x.Qty, cancellationToken);

            var validationResult = ValidateAgainstDeliveryOrder(deliveryOrder, command.Lines, alreadyInvoiced);
            if (!validationResult.IsSuccess)
                return Result.Failure<SalesInvoiceResponse>(validationResult.Error!);
        }

        var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        if (items.Count != itemIds.Count)
            return Result.Failure<SalesInvoiceResponse>(Error.NotFound("Item.NotFound", "One or more items on this invoice were not found."));

        var uomIds = command.Lines.Select(l => l.UomId).Distinct().ToList();
        var uoms = await dbContext.Uoms.Where(u => uomIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);
        if (uoms.Count != uomIds.Count)
            return Result.Failure<SalesInvoiceResponse>(Error.NotFound("Uom.NotFound", "One or more units of measure on this invoice were not found."));

        var statutoryTypeIds = command.Lines.Where(l => l.StatutoryDiscountTypeId.HasValue).Select(l => l.StatutoryDiscountTypeId!.Value).Distinct().ToList();
        var statutoryTypes = await dbContext.StatutoryDiscountTypes.Where(s => statutoryTypeIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);
        if (statutoryTypes.Count != statutoryTypeIds.Count)
            return Result.Failure<SalesInvoiceResponse>(Error.NotFound("StatutoryDiscountType.NotFound", "One or more statutory discount types on this invoice were not found."));

        if (command.CostCenterId.HasValue && !await dbContext.CostCenters.AnyAsync(c => c.Id == command.CostCenterId.Value, cancellationToken))
            return Result.Failure<SalesInvoiceResponse>(Error.NotFound("CostCenter.NotFound", $"Cost center with ID '{command.CostCenterId}' was not found."));

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "SINV"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<SalesInvoiceResponse>(numberResult.Error!);

        var dueDate = command.DueDate ?? (customer.PaymentTermsDays.HasValue ? command.InvoiceDate.AddDays(customer.PaymentTermsDays.Value) : (DateTimeOffset?)null);

        var company = await dbContext.Companies.FirstOrDefaultAsync(cancellationToken);
        // Statutory lines are always forced to a 0 discretionary DiscountPct below, so they never
        // contribute to the threshold check on their own — a statutory discount is a legal
        // entitlement, not a staff-granted concession.
        var exceedsThreshold = DiscountThresholdPolicy.ExceedsThreshold(
            company?.MaxUnapprovedDiscountPct, command.DiscountPct,
            command.Lines.Select(l => l.StatutoryDiscountTypeId.HasValue ? 0 : l.DiscountPct));
        var quickPost = company is { SalesInvoiceQuickPostEnabled: true } && !exceedsThreshold;

        var invoice = new SalesInvoice
        {
            InvoiceNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            CustomerId = command.CustomerId,
            DeliveryOrderId = command.DeliveryOrderId,
            InvoiceDate = command.InvoiceDate,
            DueDate = dueDate,
            Status = "DRAFT",
            Remarks = command.Remarks,
            DiscountPct = command.DiscountPct,
            CostCenterId = command.CostCenterId,
            CreatedBy = command.CreatedBy,
            Lines = command.Lines.Select(l => new SalesInvoiceLine
            {
                ItemId = l.ItemId,
                Qty = l.Qty,
                UomId = l.UomId,
                UnitPrice = l.UnitPrice,
                // A statutory discount overrides any discretionary discount and forces VAT_EXEMPT —
                // enforced here server-side, not just by the validator.
                DiscountPct = l.StatutoryDiscountTypeId.HasValue ? 0 : l.DiscountPct,
                DiscountSourceType = l.StatutoryDiscountTypeId.HasValue ? null : l.DiscountSourceType,
                DiscountSourceId = l.StatutoryDiscountTypeId.HasValue ? null : l.DiscountSourceId,
                VatType = l.StatutoryDiscountTypeId.HasValue ? "VAT_EXEMPT" : l.VatType,
                StatutoryDiscountTypeId = l.StatutoryDiscountTypeId,
                StatutoryIdNumber = l.StatutoryIdNumber,
                DeliveryOrderLineId = l.DeliveryOrderLineId
            }).ToList()
        };

        dbContext.SalesInvoices.Add(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);

        invoice.Customer = customer;
        foreach (var line in invoice.Lines)
        {
            line.Item = items[line.ItemId];
            line.Uom = uoms[line.UomId];
            if (line.StatutoryDiscountTypeId.HasValue)
                line.StatutoryDiscountType = statutoryTypes[line.StatutoryDiscountTypeId.Value];
        }

        if (quickPost)
        {
            var postResult = await SalesInvoicePostingService.PostAsync(dbContext, nextDocumentNumberHandler, postGlJournalHandler, invoice, cancellationToken);
            if (!postResult.IsSuccess)
                return Result.Failure<SalesInvoiceResponse>(postResult.Error!);

            invoice.Status = "POSTED";
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("SALES_INVOICE", invoice.Id.ToString(), invoice.BranchId, "CREATED", "ACTIVITY",
                quickPost ? "created this sales invoice (posted directly — quick-post enabled)" : "created this sales invoice",
                command.CreatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<SalesInvoiceResponse>(notifyResult.Error!);

        return Result.Success(SalesInvoiceMapper.ToResponse(invoice));
    }

    /// <summary>
    /// Structural checks (every line references a real line on the given delivery, for the same
    /// item) plus the actual cap: a delivery line can't be invoiced past its own QtyShipped once
    /// <paramref name="alreadyInvoicedByDeliveryOrderLine"/> (every OTHER posted invoice's claim on
    /// that line) is added to what this command itself is invoicing.
    /// </summary>
    internal static Result ValidateAgainstDeliveryOrder(
        DeliveryOrder deliveryOrder, List<SalesInvoiceLineInput> lines, Dictionary<Guid, decimal> alreadyInvoicedByDeliveryOrderLine)
    {
        if (lines.Any(l => !l.DeliveryOrderLineId.HasValue))
            return Result.Failure(Error.Validation("SalesInvoice.LineMissingDeliveryOrderLine", "Every line must reference a specific delivery line when this invoice is created against a delivery."));

        var doLinesById = deliveryOrder.Lines.ToDictionary(l => l.Id);
        foreach (var line in lines)
        {
            if (!doLinesById.TryGetValue(line.DeliveryOrderLineId!.Value, out var doLine))
                return Result.Failure(Error.Validation("SalesInvoice.InvalidDeliveryOrderLine", "One or more lines reference a delivery line that doesn't belong to the referenced delivery."));
            if (doLine.ItemId != line.ItemId)
                return Result.Failure(Error.Validation("SalesInvoice.ItemMismatch", $"A line's item must match the delivery line it references ('{doLine.Item.Code}')."));
        }

        foreach (var group in lines.GroupBy(l => l.DeliveryOrderLineId!.Value))
        {
            var doLine = doLinesById[group.Key];
            var remaining = doLine.QtyShipped - alreadyInvoicedByDeliveryOrderLine.GetValueOrDefault(group.Key);
            var requested = group.Sum(l => l.Qty);
            if (requested > remaining)
                return Result.Failure(Error.Validation("SalesInvoice.ExceedsDeliveredQty", $"This invoice bills {requested} of '{doLine.Item.Code}' but only {remaining} of delivered quantity remains uninvoiced."));
        }

        return Result.Success();
    }
}
