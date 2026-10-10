namespace ZARI.Application.Features.RouteSales.Trips.RouteSales;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Inventory.StockLedgers.Issue;
using ZARI.Application.Features.RouteSales.Trips.Shared;
using ZARI.Application.Features.Sales.CustomerPayments.Create;
using ZARI.Application.Features.Sales.CustomerPayments.GetAll;
using ZARI.Application.Features.Sales.DeliveryOrders.Shared;
using ZARI.Application.Features.Sales.PosTerminals.Shared;
using ZARI.Application.Features.Sales.SalesInvoices.Create;
using ZARI.Application.Features.Sales.SalesInvoices.GetAll;
using ZARI.Application.Features.Sales.SalesInvoices.Shared;
using ZARI.Application.Features.Sales.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Records a sale the truck handheld already completed (and printed) at a stop. ZARI's stock engine
/// commits its own transaction per call, so the three documents can't share one DB transaction;
/// instead this is an idempotent, resumable sequence driven by the RouteSale record:
///
///   0. RouteSale created first (unique on terminal + BIR number) — the idempotency key.
///   1. Delivery out of the truck warehouse (stock + COGS), id saved on the RouteSale.
///   2. Sales Invoice carrying the handheld's own number (claimed from the truck series).
///   3. Customer Payment for whatever was tendered.
///   4. Trip quantities + expected cash updated, RouteSale COMPLETED — in one save.
///
/// A retry finds the RouteSale and continues from the first step without a saved id; a document a
/// crashed attempt created but never recorded is found by the RouteSale tag in its Remarks (or, for
/// the invoice, by its unique BIR number). Everything that could reject the sale is checked before
/// step 0, because by the time this arrives the customer already holds a numbered paper invoice.
/// </summary>
public sealed class RecordRouteSaleCommandHandler(
    IAppDbContext dbContext,
    ICurrentUser currentUser,
    IPermissionService permissionService,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<IssueStockLinesCommand, Result<IssueStockLinesResponse>> issueStockLinesHandler,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<CreateSalesInvoiceCommand, Result<SalesInvoiceResponse>> createSalesInvoiceHandler,
    ICommandHandler<CreateCustomerPaymentCommand, Result<CustomerPaymentResponse>> createCustomerPaymentHandler)
    : ICommandHandler<RecordRouteSaleCommand, Result<RouteSaleResponse>>
{
    public const string RouteSaleTag = "RS";

    public async Task<Result<RouteSaleResponse>> HandleAsync(RecordRouteSaleCommand command, CancellationToken cancellationToken = default)
    {
        var trip = await dbContext.Trips.AsNoTracking().Include(t => t.Vehicle).Include(t => t.Lines).Include(t => t.SalesOrders)
            .FirstOrDefaultAsync(t => t.Id == command.TripId, cancellationToken);
        if (trip is null)
            return Fail(Error.NotFound("Trip.NotFound", $"Trip with ID '{command.TripId}' was not found."));
        if (!TripShared.IsDriver(trip, currentUser.UserId))
            return Fail(Error.Forbidden("RouteSale.NotTheDriver", "Only this trip's driver can record its sales."));
        if (!await permissionService.HasPermissionOnBranchAsync("ROUTE_SALES", FormAction.Create, trip.BranchId, cancellationToken))
            return Fail(Error.Forbidden("RouteSale.Forbidden", "You do not have permission to record route sales for this branch."));

        var terminalId = trip.Vehicle.PosTerminalId;
        var existing = await dbContext.RouteSales.FirstOrDefaultAsync(r => r.PosTerminalId == terminalId && r.BirOrSeriesNumber == command.BirOrSeriesNumber, cancellationToken);
        RecordRouteSaleCommand request;
        Guid routeSaleId;
        if (existing is not null)
        {
            if (existing.TripId != trip.Id)
                return Fail(Error.Conflict("RouteSale.NumberUsedOnAnotherTrip", $"Invoice {command.BirOrSeriesNumber} was recorded on another trip."));
            if (existing.Status == "VOID")
                return Fail(Error.Conflict("RouteSale.NumberVoided", $"Invoice {command.BirOrSeriesNumber} was voided."));
            if (existing.Status == "COMPLETED")
                return Result.Success(ToResponse(existing));
            // Resume with the request exactly as first received.
            request = JsonSerializer.Deserialize<RecordRouteSaleCommand>(existing.RequestJson)!;
            routeSaleId = existing.Id;
        }
        else
        {
            if (trip.Status != TripStatus.InTransit)
                return Fail(Error.Validation("RouteSale.TripNotOnTheRoad", "Sales can only be recorded while the trip is on the road (load accepted, not yet ended)."));

            var validation = await ValidateAsync(trip, command, cancellationToken);
            if (validation is not null)
                return Fail(validation);

            var record = new RouteSale
            {
                TripId = trip.Id,
                PosTerminalId = terminalId,
                BirOrSeriesNumber = command.BirOrSeriesNumber,
                CustomerId = command.CustomerId,
                SalesOrderId = command.SalesOrderId,
                RequestJson = JsonSerializer.Serialize(command),
                Status = "PENDING",
                SoldAt = command.SoldAt,
                CreatedBy = currentUser.UserId,
            };
            dbContext.RouteSales.Add(record);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // The same sale arriving twice at once — the other request owns it now.
                return Fail(Error.Conflict("RouteSale.InProgress", $"Invoice {command.BirOrSeriesNumber} is already being recorded — retry in a moment."));
            }
            request = command;
            routeSaleId = record.Id;
        }

        var outcome = await RunStepsAsync(trip, routeSaleId, request, cancellationToken);
        if (!outcome.IsSuccess)
        {
            dbContext.ChangeTracker.Clear();
            var failed = await dbContext.RouteSales.FirstAsync(r => r.Id == routeSaleId, cancellationToken);
            failed.LastError = Truncate(outcome.Error!.Message, 1000);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Fail(outcome.Error!);
        }

        dbContext.ChangeTracker.Clear();
        return Result.Success(ToResponse(await dbContext.RouteSales.AsNoTracking().FirstAsync(r => r.Id == routeSaleId, cancellationToken)));
    }

    // ------------------------------------------------------------------ validation (before anything is created)

    private async Task<Error?> ValidateAsync(Trip trip, RecordRouteSaleCommand command, CancellationToken ct)
    {
        var terminal = await dbContext.PosTerminals.AsNoTracking().FirstAsync(t => t.Id == trip.Vehicle.PosTerminalId, ct);
        var expected = PosTerminalSeries.Peek(terminal);
        if (command.BirOrSeriesNumber != expected)
            return Error.Validation("PosTerminal.OutOfSequence",
                $"Invoice number {command.BirOrSeriesNumber} is out of sequence — this handheld's next number is {expected}. Record or void {expected} first.");

        var customer = await dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == command.CustomerId, ct);
        if (customer is null)
            return Error.NotFound("Customer.NotFound", "The customer was not found.");

        var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.AsNoTracking().Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);
        if (items.Count != itemIds.Count)
            return Error.NotFound("Item.NotFound", "One or more items on this sale were not found.");

        // Sales-order lines: the order must be on this trip, for this customer, and not over-delivered; and
        // whatever of it is still open on this trip and not delivered now must be refused with a reason.
        var soLineIds = command.Lines.Where(l => l.SalesOrderLineId.HasValue).Select(l => l.SalesOrderLineId!.Value).Distinct().ToList();
        var refusals = command.Refusals ?? [];
        if (command.SalesOrderId is { } soId)
        {
            if (trip.SalesOrders.All(s => s.SalesOrderId != soId))
                return Error.Validation("RouteSale.SalesOrderNotOnTrip", "That sales order isn't assigned to this trip.");
            var order = await dbContext.SalesOrders.AsNoTracking().Include(o => o.Lines).ThenInclude(l => l.Item).FirstAsync(o => o.Id == soId, ct);
            if (order.CustomerId != command.CustomerId)
                return Error.Validation("RouteSale.SalesOrderOtherCustomer", $"Sales order {order.SoNo} belongs to another customer.");
            foreach (var group in command.Lines.Where(l => l.SalesOrderLineId.HasValue).GroupBy(l => l.SalesOrderLineId!.Value))
            {
                var soLine = order.Lines.FirstOrDefault(l => l.Id == group.Key);
                if (soLine is null)
                    return Error.Validation("RouteSale.InvalidSalesOrderLine", $"A line references something that isn't on sales order {order.SoNo}.");
                if (group.Any(l => l.ItemId != soLine.ItemId))
                    return Error.Validation("RouteSale.ItemMismatch", "A line's item must match the sales order line it delivers.");
            }
            var deliveredNow = command.Lines.Where(l => l.SalesOrderLineId.HasValue)
                .GroupBy(l => l.SalesOrderLineId!.Value).ToDictionary(g => g.Key, g => g.Sum(l => l.Qty));
            if (await TripRefusals.CheckAsync(dbContext, trip.Id, order, deliveredNow, refusals, requireEveryLine: true, ct) is { } refusalError)
                return refusalError;
        }
        else if (soLineIds.Count > 0 || refusals.Count > 0)
        {
            return Error.Validation("RouteSale.SalesOrderNotOnTrip", "Order lines and refusals must name the sales order (assigned to this trip) they belong to.");
        }

        // Stock: the trip's own documents say exactly what good stock is on the truck.
        foreach (var group in command.Lines.GroupBy(l => l.ItemId))
        {
            var line = trip.Lines.FirstOrDefault(l => l.ItemId == group.Key);
            var onTruck = line is null ? 0 : TripShared.ExpectedGoodOnTruck(line);
            if (group.Sum(l => l.Qty) > onTruck && !items[group.Key].AllowNegativeStock)
                return Error.Validation("RouteSale.NotOnTruck", $"{items[group.Key].Code}: only {onTruck} on the truck.");
        }

        // Discounts: the same two rules CreateSalesInvoiceCommandHandler applies — checked here, since
        // failing them at the invoice step would leave the delivery already posted.
        var company = await dbContext.Companies.AsNoTracking().FirstOrDefaultAsync(ct);
        if (DiscountThresholdPolicy.ExceedsThreshold(company?.MaxUnapprovedDiscountPct, command.DiscountPct,
                command.Lines.Select(l => l.StatutoryDiscountTypeId.HasValue ? 0 : l.DiscountPct)))
            return Error.Validation("RouteSale.DiscountNeedsApproval", "A discount on this sale is above what can be given without approval.");
        if (command.Lines.Any(l => !l.StatutoryDiscountTypeId.HasValue && l.DiscountPct > 0))
        {
            var rules = await DiscountEligibility.GetActiveRulesAsync(dbContext, trip.BranchId, ct);
            var asOf = DateOnly.FromDateTime(command.SoldAt.Date);
            var ineligible = command.Lines.Where(l => !l.StatutoryDiscountTypeId.HasValue && l.DiscountPct > 0
                && !DiscountEligibility.IsEligible(rules, l.ItemId, items[l.ItemId].CategoryId, asOf, l.Qty)).Select(l => items[l.ItemId].Code).ToList();
            if (ineligible.Count > 0)
                return Error.Validation("RouteSale.DiscountNotAllowed", $"No active discount rule covers: {string.Join(", ", ineligible.Distinct())}.");
        }

        // Tenders and credit.
        var total = await TotalAsync(command, items, ct);
        if (total is null)
            return Error.NotFound("StatutoryDiscountType.NotFound", "A statutory discount type on this sale was not found.");
        var methodIds = command.Tenders.Select(t => t.PaymentMethodId).Distinct().ToList();
        var methods = await dbContext.PaymentMethods.AsNoTracking().Where(m => methodIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id, ct);
        if (methods.Count != methodIds.Count)
            return Error.NotFound("PaymentMethod.NotFound", "One or more payment methods were not found.");
        var tendered = command.Tenders.Sum(t => t.Amount);
        var change = Math.Max(0, Math.Round(tendered - total.Value, 4));
        if (change > 0 && !command.Tenders.Any(t => methods[t.PaymentMethodId].Code == "CASH" && t.Amount >= change))
            return Error.Validation("RouteSale.InvalidChange", "Change can only be given out of a single cash tender that covers it.");

        var unpaid = Math.Max(0, total.Value - tendered);
        if (unpaid > 0 && customer.CreditLimit is { } limit)
        {
            var open = (await CustomerBalances.OpenArAsync(dbContext, [customer.Id], ct))[customer.Id];
            if (open + unpaid > limit)
                return Error.Validation("RouteSale.CreditLimitExceeded",
                    $"{customer.Name} would owe {open + unpaid:N2}, over their credit limit of {limit:N2}.");
        }

        return null;
    }

    private async Task<decimal?> TotalAsync(RecordRouteSaleCommand command, Dictionary<Guid, Item> items, CancellationToken ct)
    {
        var statutoryIds = command.Lines.Where(l => l.StatutoryDiscountTypeId.HasValue).Select(l => l.StatutoryDiscountTypeId!.Value).Distinct().ToList();
        var statutory = await dbContext.StatutoryDiscountTypes.AsNoTracking().Where(s => statutoryIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, ct);
        if (statutory.Count != statutoryIds.Count) return null;

        decimal total = 0;
        foreach (var l in command.Lines)
        {
            var calc = SalesInvoiceLineCalculator.Calculate(new SalesInvoiceLineCalculator.LineInput(l.Qty, l.UnitPrice, l.DiscountPct,
                l.VatType ?? items[l.ItemId].VatType, l.StatutoryDiscountTypeId is { } sid ? statutory[sid].DiscountPct : null));
            total += Math.Round(calc.NetAmount * (1 - (command.DiscountPct ?? 0) / 100m), 4);
        }
        return Math.Round(total, 4);
    }

    // ------------------------------------------------------------------ the resumable steps

    private async Task<Result> RunStepsAsync(Trip trip, Guid routeSaleId, RecordRouteSaleCommand request, CancellationToken ct)
    {
        var tag = TripShared.Tag(RouteSaleTag, routeSaleId);
        var sale = await dbContext.RouteSales.AsNoTracking().FirstAsync(r => r.Id == routeSaleId, ct);

        // 1. Delivery out of the truck.
        if (sale.DeliveryOrderId is null)
        {
            var delivery = await DeliverAsync(trip, request, tag, ct);
            if (!delivery.IsSuccess) return Result.Failure(delivery.Error!);
            await SaveOnSaleAsync(routeSaleId, s => s.DeliveryOrderId = delivery.Value, ct);
        }
        sale = await dbContext.RouteSales.AsNoTracking().FirstAsync(r => r.Id == routeSaleId, ct);

        // 2. The invoice, with the handheld's own BIR number.
        if (sale.SalesInvoiceId is null)
        {
            var invoiceId = await dbContext.SalesInvoices.Where(i => i.BirOrSeriesNumber == request.BirOrSeriesNumber)
                .Select(i => (Guid?)i.Id).FirstOrDefaultAsync(ct);
            if (invoiceId is null)
            {
                var deliveryLines = await dbContext.DeliveryOrderLines.AsNoTracking().Where(l => l.DeliveryOrderId == sale.DeliveryOrderId).ToListAsync(ct);
                var items = await ItemsAsync(request, ct);
                var used = new HashSet<Guid>();
                var invoiceLines = new List<SalesInvoiceLineInput>();
                foreach (var l in request.Lines)
                {
                    var doLine = deliveryLines.First(d => !used.Contains(d.Id) && d.ItemId == l.ItemId
                        && d.SalesOrderLineId == l.SalesOrderLineId && d.QtyShipped == l.Qty);
                    used.Add(doLine.Id);
                    invoiceLines.Add(new SalesInvoiceLineInput(l.ItemId, l.Qty, doLine.UomId, l.UnitPrice, l.DiscountPct, null, null,
                        l.VatType ?? items[l.ItemId].VatType, l.StatutoryDiscountTypeId, l.StatutoryIdNumber, doLine.Id, null));
                }

                var invoice = await createSalesInvoiceHandler.HandleAsync(new CreateSalesInvoiceCommand(
                    trip.BranchId, request.CustomerId, sale.DeliveryOrderId, request.SoldAt, null, $"Route sale {tag}", request.DiscountPct,
                    null, trip.DriverUserId, invoiceLines, ForceQuickPost: true, PosTerminalId: trip.Vehicle.PosTerminalId,
                    PresetBirOrSeriesNumber: request.BirOrSeriesNumber), ct);
                if (!invoice.IsSuccess) return Result.Failure(invoice.Error!);
                invoiceId = invoice.Value!.Id;
            }
            dbContext.ChangeTracker.Clear();
            await SaveOnSaleAsync(routeSaleId, s => s.SalesInvoiceId = invoiceId, ct);
        }
        sale = await dbContext.RouteSales.AsNoTracking().FirstAsync(r => r.Id == routeSaleId, ct);

        var posted = await dbContext.SalesInvoices.AsNoTracking().Include(i => i.Lines).ThenInclude(l => l.StatutoryDiscountType)
            .FirstAsync(i => i.Id == sale.SalesInvoiceId, ct);
        var total = SalesInvoicePaymentBalance.GetInvoiceTotal(posted);

        // 3. Payment for whatever was tendered (change comes out of one cash tender, like POS).
        var methods = await dbContext.PaymentMethods.AsNoTracking().ToDictionaryAsync(m => m.Id, ct);
        var tendered = request.Tenders.Sum(t => t.Amount);
        var change = Math.Max(0, Math.Round(tendered - total, 4));
        var tenders = request.Tenders.ToList();
        if (change > 0)
        {
            var cashIndex = tenders.FindIndex(t => methods[t.PaymentMethodId].Code == "CASH" && t.Amount >= change);
            tenders[cashIndex] = tenders[cashIndex] with { Amount = Math.Round(tenders[cashIndex].Amount - change, 4) };
            tenders = tenders.Where(t => t.Amount > 0).ToList();
        }
        var applied = Math.Min(tendered, total);
        if (sale.CustomerPaymentId is null && applied > 0)
        {
            var paymentId = await dbContext.CustomerPayments.Where(p => p.Remarks != null && p.Remarks.Contains(tag))
                .Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct);
            if (paymentId is null)
            {
                var payment = await createCustomerPaymentHandler.HandleAsync(new CreateCustomerPaymentCommand(
                    trip.BranchId, request.CustomerId, request.SoldAt, $"Route sale payment {tag}", null, trip.DriverUserId,
                    [new CustomerPaymentLineInput(posted.Id, applied)], Tenders: tenders, ForceQuickPost: true), ct);
                if (!payment.IsSuccess) return Result.Failure(payment.Error!);
                paymentId = payment.Value!.Id;
            }
            dbContext.ChangeTracker.Clear();
            await SaveOnSaleAsync(routeSaleId, s => s.CustomerPaymentId = paymentId, ct);
        }

        // 4. Trip quantities, expected cash and completion — one save, so it happens exactly once.
        dbContext.ChangeTracker.Clear();
        var cashKept = tenders.Where(t => methods[t.PaymentMethodId].Code == "CASH").Sum(t => t.Amount);
        var tripToUpdate = await dbContext.Trips.Include(t => t.Lines).FirstAsync(t => t.Id == trip.Id, ct);
        var itemsById = await ItemsAsync(request, ct);
        foreach (var l in request.Lines)
        {
            var line = TripShared.LineFor(tripToUpdate, itemsById[l.ItemId]);
            if (l.SalesOrderLineId.HasValue) line.QtyDelivered += l.Qty;
            else line.QtyVanSold += l.Qty;
        }
        tripToUpdate.CashExpected += cashKept;

        // Whatever of the order wasn't delivered here, and why — stays on the truck, the order line stays open.
        if (request.Refusals is { Count: > 0 } refusals && request.SalesOrderId is { } soId
            && !await dbContext.TripRefusals.AnyAsync(r => r.ClientRef == routeSaleId, ct))
        {
            var soLines = await dbContext.SalesOrderLines.AsNoTracking().Include(l => l.Item)
                .Where(l => l.SalesOrderId == soId).ToDictionaryAsync(l => l.Id, ct);
            dbContext.TripRefusals.Add(TripRefusals.Build(trip.Id, routeSaleId, request.CustomerId, soId, routeSaleId, request.SoldAt, refusals,
                soLines.ToDictionary(x => x.Key, x => x.Value.ItemId), trip.DriverUserId));
            foreach (var r in refusals)
                TripShared.LineFor(tripToUpdate, soLines[r.SalesOrderLineId].Item).QtyRefused += r.Qty;
        }

        var done = await dbContext.RouteSales.FirstAsync(r => r.Id == routeSaleId, ct);
        done.Status = "COMPLETED";
        done.CompletedAt = DateTimeOffset.UtcNow;
        done.InvoiceTotal = total;
        done.AmountPaid = applied;
        done.CashCollected = cashKept;
        done.ReceivedByName = request.ReceivedByName;
        done.Latitude = request.Latitude;
        done.Longitude = request.Longitude;
        done.LastError = null;
        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<Result<Guid>> DeliverAsync(Trip trip, RecordRouteSaleCommand request, string tag, CancellationToken ct)
    {
        var order = await dbContext.DeliveryOrders.Include(d => d.Lines).ThenInclude(l => l.Item).Include(d => d.Customer)
            .FirstOrDefaultAsync(d => d.Remarks != null && d.Remarks.Contains(tag), ct);
        if (order is null)
        {
            var number = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(trip.BranchId, "DO"), ct);
            if (!number.IsSuccess) return Result.Failure<Guid>(number.Error!);
            var items = await ItemsAsync(request, ct);
            order = new DeliveryOrder
            {
                DoNo = number.Value!.DocumentNumber,
                BranchId = trip.BranchId,
                WarehouseId = trip.Vehicle.WarehouseId,
                CustomerId = request.CustomerId,
                SalesOrderId = request.SalesOrderId,
                DeliveryDate = request.SoldAt,
                Status = "DRAFT",
                Remarks = $"Route sale {tag}",
                CreatedBy = trip.DriverUserId,
                // Unlike a back-office delivery, a stop may mix order lines and van-sale lines on one
                // receipt — van-sale lines simply carry no SalesOrderLineId.
                Lines = request.Lines.Select(l => new DeliveryOrderLine
                {
                    ItemId = l.ItemId,
                    QtyShipped = l.Qty,
                    UomId = items[l.ItemId].BaseUomId,
                    SalesOrderLineId = l.SalesOrderLineId,
                }).ToList(),
            };
            dbContext.DeliveryOrders.Add(order);
            await dbContext.SaveChangesAsync(ct);
            order = await dbContext.DeliveryOrders.Include(d => d.Lines).ThenInclude(l => l.Item).Include(d => d.Customer).FirstAsync(d => d.Id == order.Id, ct);
        }

        if (order.Status != "POSTED")
        {
            var orderId = order.Id;
            // A crash after the COGS journal but before the status flip must not journal twice.
            var journaled = await dbContext.GlJournals.AnyAsync(j => j.SourceReferenceTable == "DeliveryOrder" && j.SourceReferenceId == orderId.ToString(), ct);
            if (!journaled)
            {
                var post = await DeliveryPostingService.PostStockAndGlAsync(dbContext, issueStockLinesHandler, postGlJournalHandler, order, ct);
                if (!post.IsSuccess) return Result.Failure<Guid>(post.Error!);
            }
            dbContext.ChangeTracker.Clear();
            var reloaded = await dbContext.DeliveryOrders.FirstAsync(d => d.Id == orderId, ct);
            reloaded.Status = "POSTED";
            await dbContext.SaveChangesAsync(ct);
        }
        return Result.Success(order.Id);
    }

    private async Task<Dictionary<Guid, Item>> ItemsAsync(RecordRouteSaleCommand request, CancellationToken ct)
    {
        var ids = request.Lines.Select(l => l.ItemId).Distinct().ToList();
        return await dbContext.Items.AsNoTracking().Where(i => ids.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);
    }

    private async Task SaveOnSaleAsync(Guid routeSaleId, Action<RouteSale> apply, CancellationToken ct)
    {
        var sale = await dbContext.RouteSales.FirstAsync(r => r.Id == routeSaleId, ct);
        apply(sale);
        await dbContext.SaveChangesAsync(ct);
    }

    internal static RouteSaleResponse ToResponse(RouteSale r)
    {
        var request = JsonSerializer.Deserialize<RecordRouteSaleCommand>(r.RequestJson);
        var tendered = request?.Tenders?.Sum(t => t.Amount) ?? 0;
        return new RouteSaleResponse(r.Id, r.Status, r.BirOrSeriesNumber, r.DeliveryOrderId, r.SalesInvoiceId, r.CustomerPaymentId,
            r.InvoiceTotal, r.AmountPaid, r.Status == "COMPLETED" ? Math.Max(0, Math.Round(tendered - r.InvoiceTotal, 4)) : 0,
            Math.Max(0, r.InvoiceTotal - r.AmountPaid));
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

    private static Result<RouteSaleResponse> Fail(Error error) => Result.Failure<RouteSaleResponse>(error);
}
