namespace ZARI.Application.Features.Sales.SalesInvoices.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesInvoices.Create;
using ZARI.Application.Features.Sales.SalesInvoices.GetAll;
using ZARI.Application.Features.Sales.SalesInvoices.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateSalesInvoiceCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<UpdateSalesInvoiceCommand, Result<SalesInvoiceResponse>>
{
    public async Task<Result<SalesInvoiceResponse>> HandleAsync(UpdateSalesInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.SalesInvoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);

        if (invoice is null)
            return Result.Failure<SalesInvoiceResponse>(Error.NotFound("SalesInvoice.NotFound", $"Sales invoice with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("SALES_INVOICES", FormAction.Edit, invoice.BranchId, cancellationToken))
            return Result.Failure<SalesInvoiceResponse>(Error.Forbidden("SalesInvoice.Forbidden", "You do not have permission to update sales invoices for this branch."));

        if (invoice.Status != "DRAFT")
            return Result.Failure<SalesInvoiceResponse>(Error.Validation("SalesInvoice.NotDraft", "Only draft sales invoices can be edited."));

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
            // Excludes this invoice's own (pre-update) lines from the "already invoiced" tally —
            // this invoice is still DRAFT (not POSTED) so it's naturally excluded already.
            var alreadyInvoiced = await dbContext.SalesInvoiceLines
                .Where(l => l.DeliveryOrderLineId.HasValue && referencedLineIds.Contains(l.DeliveryOrderLineId.Value) && l.SalesInvoice.Status == "POSTED")
                .GroupBy(l => l.DeliveryOrderLineId!.Value)
                .Select(g => new { DeliveryOrderLineId = g.Key, Qty = g.Sum(l => l.Qty) })
                .ToDictionaryAsync(x => x.DeliveryOrderLineId, x => x.Qty, cancellationToken);

            var validationResult = CreateSalesInvoiceCommandHandler.ValidateAgainstDeliveryOrder(deliveryOrder, command.Lines, alreadyInvoiced);
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

        invoice.BranchId = command.BranchId;
        invoice.CustomerId = command.CustomerId;
        invoice.DeliveryOrderId = command.DeliveryOrderId;
        invoice.InvoiceDate = command.InvoiceDate;
        invoice.DueDate = command.DueDate ?? (customer.PaymentTermsDays.HasValue ? command.InvoiceDate.AddDays(customer.PaymentTermsDays.Value) : (DateTimeOffset?)null);
        invoice.Remarks = command.Remarks;
        invoice.DiscountPct = command.DiscountPct;
        invoice.CostCenterId = command.CostCenterId;

        invoice.Lines.Clear();
        foreach (var line in command.Lines)
        {
            invoice.Lines.Add(new SalesInvoiceLine
            {
                ItemId = line.ItemId,
                Qty = line.Qty,
                UomId = line.UomId,
                UnitPrice = line.UnitPrice,
                DiscountPct = line.StatutoryDiscountTypeId.HasValue ? 0 : line.DiscountPct,
                DiscountSourceType = line.StatutoryDiscountTypeId.HasValue ? null : line.DiscountSourceType,
                DiscountSourceId = line.StatutoryDiscountTypeId.HasValue ? null : line.DiscountSourceId,
                VatType = line.StatutoryDiscountTypeId.HasValue ? "VAT_EXEMPT" : line.VatType,
                StatutoryDiscountTypeId = line.StatutoryDiscountTypeId,
                StatutoryIdNumber = line.StatutoryIdNumber,
                DeliveryOrderLineId = line.DeliveryOrderLineId
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        invoice.Customer = customer;
        foreach (var line in invoice.Lines)
        {
            line.Item = items[line.ItemId];
            line.Uom = uoms[line.UomId];
            if (line.StatutoryDiscountTypeId.HasValue)
                line.StatutoryDiscountType = statutoryTypes[line.StatutoryDiscountTypeId.Value];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("SALES_INVOICE", invoice.Id.ToString(), invoice.BranchId, "UPDATED", "ACTIVITY",
                "updated this sales invoice", command.UpdatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<SalesInvoiceResponse>(notifyResult.Error!);

        return Result.Success(SalesInvoiceMapper.ToResponse(invoice));
    }
}
