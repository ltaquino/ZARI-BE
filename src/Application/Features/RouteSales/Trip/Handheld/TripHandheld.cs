namespace ZARI.Application.Features.RouteSales.Trips.Handheld;

using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.Trips.LoadWorkflow;
using ZARI.Application.Features.RouteSales.Trips.RouteSales;
using ZARI.Application.Features.RouteSales.Trips.Shared;
using ZARI.Application.Features.Sales.CustomerPayments.Create;
using ZARI.Application.Features.Sales.CustomerPayments.GetAll;
using ZARI.Application.Features.Sales.PosTerminals.Shared;
using ZARI.Application.Features.Sales.SalesReturns.Create;
using ZARI.Application.Features.Sales.SalesReturns.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

// ================================================================ My trips (the handheld's download)

public sealed record DeviceSalesOrderLine(Guid SalesOrderLineId, Guid ItemId, Guid UomId, decimal Qty, decimal QtyRemaining, decimal UnitPrice, decimal DiscountPct);
public sealed record DeviceSalesOrder(Guid Id, string SoNo, Guid CustomerId, string CustomerName, decimal? DiscountPct, List<DeviceSalesOrderLine> Lines);
public sealed record DeviceStop(Guid CustomerId, string CustomerName, int Sequence, string? Notes, decimal OpenBalance, decimal? CreditLimit,
    string? Address = null, string? Tin = null, List<OpenInvoice>? OpenInvoices = null);

/// <summary>What a BIR invoice printed on the truck must carry about the seller — the driver can't read Company
/// Settings, so it comes with the trip.</summary>
public sealed record DeviceInvoiceHeader(
    string RegisteredName,
    string? TradeName,
    string? Tin,
    string? RegisteredAddress,
    string? VatRegistrationType,
    string BranchName,
    string? BranchAddress,
    string? BirBranchCode);

/// <summary>Everything the handheld needs to work the trip offline.</summary>
public sealed record DeviceTripBundle(
    TripResponse Trip,
    string NextInvoiceNumber,
    string? MachineIdentificationNumber,
    string? MachineSerialNumber,
    string? PosPermitNumber,
    List<DeviceSalesOrder> SalesOrders,
    List<DeviceStop> Stops,
    List<string> RecordedInvoiceNumbers,
    /// <summary>The reasons the handheld offers when an ordered quantity isn't delivered.</summary>
    IReadOnlyList<RefusalReasonOption> RefusalReasons,
    DeviceInvoiceHeader? InvoiceHeader = null,
    DateTime? PosPermitDateIssued = null);

public sealed record GetMyTripsQuery : IQuery<Result<List<DeviceTripBundle>>>;

public sealed class GetMyTripsQueryHandler(IAppDbContext dbContext, ICurrentUser currentUser) : IQueryHandler<GetMyTripsQuery, Result<List<DeviceTripBundle>>>
{
    public async Task<Result<List<DeviceTripBundle>>> HandleAsync(GetMyTripsQuery query, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result.Failure<List<DeviceTripBundle>>(Error.Forbidden("Trip.NotSignedIn", "Sign in to see your trips."));

        var trips = await dbContext.Trips.AsNoTracking().WithDetails()
            .Where(t => t.DriverUserId == userId && TripStatus.OnTheRoad.Contains(t.Status))
            .OrderBy(t => t.TripDate).ToListAsync(cancellationToken);

        var bundles = new List<DeviceTripBundle>();
        var company = await dbContext.Companies.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        foreach (var trip in trips)
        {
            var branch = await dbContext.Branches.AsNoTracking().FirstAsync(b => b.Id == trip.BranchId, cancellationToken);
            var header = new DeviceInvoiceHeader(company?.Name ?? "", company?.TradeName, company?.TaxId, company?.RegisteredAddress,
                company?.VatRegistrationType, branch.Name, branch.Address, branch.BirBranchCode);
            var terminal = await dbContext.PosTerminals.AsNoTracking().FirstAsync(t => t.Id == trip.Vehicle.PosTerminalId, cancellationToken);
            var soIds = trip.SalesOrders.Select(s => s.SalesOrderId).ToList();
            var orders = await dbContext.SalesOrders.AsNoTracking().Include(o => o.Lines).Include(o => o.Customer)
                .Where(o => soIds.Contains(o.Id)).ToListAsync(cancellationToken);
            var soLineIds = orders.SelectMany(o => o.Lines).Select(l => l.Id).ToList();
            var delivered = await dbContext.DeliveryOrderLines
                .Where(l => l.SalesOrderLineId.HasValue && soLineIds.Contains(l.SalesOrderLineId.Value) && l.DeliveryOrder.Status == "POSTED")
                .GroupBy(l => l.SalesOrderLineId!.Value).Select(g => new { g.Key, Qty = g.Sum(l => l.QtyShipped) })
                .ToDictionaryAsync(x => x.Key, x => x.Qty, cancellationToken);
            var refused = await TripShared.RefusedOnTripAsync(dbContext, trip.Id, soLineIds, cancellationToken);

            var stops = new List<(Guid CustomerId, string Name, int Seq, string? Notes, decimal? Limit)>();
            if (trip.RouteId is { } routeId)
            {
                stops = (await dbContext.DeliveryRouteStops.AsNoTracking().Include(s => s.Customer).Where(s => s.RouteId == routeId).ToListAsync(cancellationToken))
                    .Select(s => (s.CustomerId, s.Customer.Name, s.Sequence, s.Notes, s.Customer.CreditLimit)).ToList();
            }
            // Customers with an order on the trip but not on the route still need a stop.
            foreach (var o in orders.Where(o => stops.All(s => s.CustomerId != o.CustomerId)))
                stops.Add((o.CustomerId, o.Customer.Name, stops.Count + 1, null, o.Customer.CreditLimit));
            var stopCustomerIds = stops.Select(s => s.CustomerId).ToList();
            var openInvoices = await CustomerBalances.OpenInvoicesAsync(dbContext, stopCustomerIds, cancellationToken);
            var customerInfo = await dbContext.Customers.AsNoTracking().Where(c => stopCustomerIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Address, c.Tin }).ToDictionaryAsync(c => c.Id, cancellationToken);

            var recorded = await dbContext.RouteSales.Where(r => r.TripId == trip.Id).Select(r => r.BirOrSeriesNumber).ToListAsync(cancellationToken);

            bundles.Add(new DeviceTripBundle(
                TripShared.ToResponse(trip),
                PosTerminalSeries.Peek(terminal),
                terminal.MachineIdentificationNumber, terminal.MachineSerialNumber, terminal.PosPermitNumber,
                orders.Select(o => new DeviceSalesOrder(o.Id, o.SoNo, o.CustomerId, o.Customer.Name, o.DiscountPct,
                    o.Lines.Select(l => new DeviceSalesOrderLine(l.Id, l.ItemId, l.UomId, l.Qty, l.Qty - delivered.GetValueOrDefault(l.Id) - refused.GetValueOrDefault(l.Id), l.UnitPrice, l.DiscountPct))
                        .Where(l => l.QtyRemaining > 0).ToList())).ToList(),
                stops.OrderBy(s => s.Seq).Select(s => new DeviceStop(s.CustomerId, s.Name, s.Seq, s.Notes,
                    openInvoices[s.CustomerId].Sum(i => i.Balance), s.Limit,
                    customerInfo.GetValueOrDefault(s.CustomerId)?.Address, customerInfo.GetValueOrDefault(s.CustomerId)?.Tin,
                    openInvoices[s.CustomerId])).ToList(),
                recorded,
                TripRefusalReason.All,
                header,
                terminal.PosPermitDateIssued));
        }
        return Result.Success(bundles);
    }
}

// ================================================================ Accept load: LOADED -> IN_TRANSIT

public sealed record TripCountInput(Guid ItemId, decimal QtyCounted);
public sealed record TripCountDiscrepancy(Guid ItemId, string ItemCode, decimal Expected, decimal Counted);
public sealed record AcceptTripLoadResponse(TripResponse Trip, List<TripCountDiscrepancy> Discrepancies);

/// <summary>The driver counts what's on the truck before leaving. Mismatches are recorded on the
/// trip (QtyLoadCheck) and flagged to the branch — the trip still departs; they're settled at close.</summary>
public sealed record AcceptTripLoadCommand(Guid TripId, List<TripCountInput> Counts) : ICommand<Result<AcceptTripLoadResponse>>;

public sealed class AcceptTripLoadCommandHandler(
    IAppDbContext dbContext, ICurrentUser currentUser,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler)
    : ICommandHandler<AcceptTripLoadCommand, Result<AcceptTripLoadResponse>>
{
    public async Task<Result<AcceptTripLoadResponse>> HandleAsync(AcceptTripLoadCommand command, CancellationToken cancellationToken = default)
    {
        var trip = await dbContext.Trips.Include(t => t.Lines).ThenInclude(l => l.Item).FirstOrDefaultAsync(t => t.Id == command.TripId, cancellationToken);
        if (trip is null)
            return Result.Failure<AcceptTripLoadResponse>(Error.NotFound("Trip.NotFound", $"Trip with ID '{command.TripId}' was not found."));
        if (!TripShared.IsDriver(trip, currentUser.UserId))
            return Result.Failure<AcceptTripLoadResponse>(Error.Forbidden("Trip.NotTheDriver", "Only this trip's driver can accept its load."));
        if (trip.Status == TripStatus.InTransit)
            return Result.Success(new AcceptTripLoadResponse(await TripShared.LoadResponseAsync(dbContext, trip.Id, cancellationToken), []));
        if (trip.Status != TripStatus.Loaded)
            return Result.Failure<AcceptTripLoadResponse>(Error.Validation("Trip.NotLoaded", "Only a loaded trip can be accepted."));

        var counts = command.Counts.GroupBy(c => c.ItemId).ToDictionary(g => g.Key, g => g.Sum(c => c.QtyCounted));
        var discrepancies = new List<TripCountDiscrepancy>();
        foreach (var line in trip.Lines.Where(l => l.QtyLoaded > 0 || counts.ContainsKey(l.ItemId)))
        {
            var counted = counts.GetValueOrDefault(line.ItemId);
            line.QtyLoadCheck = counted;
            if (counted != line.QtyLoaded)
                discrepancies.Add(new TripCountDiscrepancy(line.ItemId, line.Item.Code, line.QtyLoaded, counted));
        }

        trip.Status = TripStatus.InTransit;
        trip.LoadAcceptedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (discrepancies.Count > 0)
        {
            await createNotificationHandler.HandleAsync(new CreateNotificationCommand("TRIP", trip.Id.ToString(), trip.BranchId, "LOAD_DISCREPANCY",
                "ACTIVITY", TripShared.NotificationMessage($"accepted the load with {discrepancies.Count} count discrepancy(ies): "
                    + string.Join(", ", discrepancies.Select(d => $"{d.ItemCode} loaded {d.Expected}, counted {d.Counted}"))), currentUser.UserId), cancellationToken);
        }
        return Result.Success(new AcceptTripLoadResponse(await TripShared.LoadResponseAsync(dbContext, trip.Id, cancellationToken), discrepancies));
    }
}

// ================================================================ Void an invoice number

/// <summary>
/// An invoice printed then cancelled at the door. Its number is consumed (BIR: a void keeps its
/// number) so the series stays unbroken and the next sale can be recorded. Idempotent.
/// </summary>
public sealed record VoidRouteInvoiceCommand(Guid TripId, string BirOrSeriesNumber, string Reason) : ICommand<Result<RouteSaleResponse>>;

public sealed class VoidRouteInvoiceCommandHandler(IAppDbContext dbContext, ICurrentUser currentUser, IPermissionService permissionService)
    : ICommandHandler<VoidRouteInvoiceCommand, Result<RouteSaleResponse>>
{
    public async Task<Result<RouteSaleResponse>> HandleAsync(VoidRouteInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            return Result.Failure<RouteSaleResponse>(Error.Validation("RouteSale.VoidReasonRequired", "A reason is required to void an invoice."));
        var trip = await dbContext.Trips.AsNoTracking().Include(t => t.Vehicle).FirstOrDefaultAsync(t => t.Id == command.TripId, cancellationToken);
        if (trip is null)
            return Result.Failure<RouteSaleResponse>(Error.NotFound("Trip.NotFound", $"Trip with ID '{command.TripId}' was not found."));
        if (!TripShared.IsDriver(trip, currentUser.UserId)
            && !await permissionService.HasPermissionOnBranchAsync("TRIPS", FormAction.Approve, trip.BranchId, cancellationToken))
            return Result.Failure<RouteSaleResponse>(Error.Forbidden("RouteSale.NotTheDriver", "Only this trip's driver or a branch approver can void its invoices."));

        var terminalId = trip.Vehicle.PosTerminalId;
        var existing = await dbContext.RouteSales.FirstOrDefaultAsync(r => r.PosTerminalId == terminalId && r.BirOrSeriesNumber == command.BirOrSeriesNumber, cancellationToken);
        if (existing is { Status: "VOID" })
            return Result.Success(RecordRouteSaleCommandHandler.ToResponse(existing));
        if (existing is { Status: "COMPLETED" } || existing?.DeliveryOrderId is not null)
            return Result.Failure<RouteSaleResponse>(Error.Conflict("RouteSale.AlreadyRecorded",
                $"Invoice {command.BirOrSeriesNumber} has already been (partly) recorded — it must be cancelled through the normal sales return/cancellation process."));

        var claim = await PosTerminalSeries.ClaimAsync(dbContext, terminalId, command.BirOrSeriesNumber, cancellationToken);
        if (!claim.IsSuccess)
            return Result.Failure<RouteSaleResponse>(claim.Error!);

        var record = existing ?? new RouteSale
        {
            TripId = trip.Id, PosTerminalId = terminalId, BirOrSeriesNumber = command.BirOrSeriesNumber,
            RequestJson = "{}", SoldAt = DateTimeOffset.UtcNow, CreatedBy = currentUser.UserId,
        };
        record.Status = "VOID";
        record.VoidReason = command.Reason.Trim();
        record.CompletedAt = DateTimeOffset.UtcNow;
        if (existing is null) dbContext.RouteSales.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(RecordRouteSaleCommandHandler.ToResponse(record));
    }
}

// ================================================================ Customer return onto the truck

public sealed record TripReturnLineInput(Guid ItemId, decimal Qty, decimal UnitPrice, bool IsDamaged);

/// <summary>Goods a customer hands back: a Sales Return into the truck warehouse (credit to their
/// account). Damaged goods ride back too and go to the branch Damaged warehouse at trip close.
/// Idempotent on ClientRef.</summary>
public sealed record RecordTripReturnCommand(Guid TripId, Guid ClientRef, Guid CustomerId, DateTimeOffset ReturnedAt, string? Reason,
    List<TripReturnLineInput> Lines) : ICommand<Result<Guid>>;

public sealed class RecordTripReturnValidator : AbstractValidator<RecordTripReturnCommand>
{
    public RecordTripReturnValidator()
    {
        RuleFor(x => x.ClientRef).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(300);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.ItemId).NotEmpty();
            l.RuleFor(x => x.Qty).GreaterThan(0);
            l.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class RecordTripReturnCommandHandler(
    IAppDbContext dbContext, ICurrentUser currentUser, IPermissionService permissionService,
    ICommandHandler<CreateSalesReturnCommand, Result<SalesReturnResponse>> createSalesReturnHandler)
    : ICommandHandler<RecordTripReturnCommand, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(RecordTripReturnCommand command, CancellationToken cancellationToken = default)
    {
        var trip = await dbContext.Trips.AsNoTracking().Include(t => t.Vehicle).FirstOrDefaultAsync(t => t.Id == command.TripId, cancellationToken);
        if (trip is null)
            return Result.Failure<Guid>(Error.NotFound("Trip.NotFound", $"Trip with ID '{command.TripId}' was not found."));
        if (!TripShared.IsDriver(trip, currentUser.UserId))
            return Result.Failure<Guid>(Error.Forbidden("Trip.NotTheDriver", "Only this trip's driver can record its returns."));
        if (!await permissionService.HasPermissionOnBranchAsync("ROUTE_SALES", FormAction.Create, trip.BranchId, cancellationToken))
            return Result.Failure<Guid>(Error.Forbidden("RouteSale.Forbidden", "You do not have permission to record route sales for this branch."));

        var record = await dbContext.TripReturns.FirstOrDefaultAsync(r => r.ClientRef == command.ClientRef, cancellationToken);
        if (record is { Status: "COMPLETED" })
            return Result.Success(record.Id);
        if (record is null)
        {
            if (trip.Status != TripStatus.InTransit)
                return Result.Failure<Guid>(Error.Validation("Trip.NotOnTheRoad", "Returns can only be recorded while the trip is on the road."));
            if (!await dbContext.Customers.AnyAsync(c => c.Id == command.CustomerId, cancellationToken))
                return Result.Failure<Guid>(Error.NotFound("Customer.NotFound", "The customer was not found."));
            record = new TripReturn
            {
                TripId = trip.Id, ClientRef = command.ClientRef, CustomerId = command.CustomerId, Status = "PENDING", Reason = command.Reason,
                ReturnedAt = command.ReturnedAt, CreatedBy = currentUser.UserId,
                Lines = command.Lines.Select(l => new TripReturnLine { ItemId = l.ItemId, Qty = l.Qty, UnitPrice = l.UnitPrice, IsDamaged = l.IsDamaged }).ToList(),
            };
            dbContext.TripReturns.Add(record);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        var recordId = record.Id;
        var tag = TripShared.Tag("TR", recordId);

        var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.AsNoTracking().Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        if (items.Count != itemIds.Count)
            return Result.Failure<Guid>(Error.NotFound("Item.NotFound", "One or more returned items were not found."));

        var salesReturnId = record.SalesReturnId ?? await dbContext.SalesReturns.Where(s => s.Remarks != null && s.Remarks.Contains(tag))
            .Select(s => (Guid?)s.Id).FirstOrDefaultAsync(cancellationToken);
        if (salesReturnId is null)
        {
            var created = await createSalesReturnHandler.HandleAsync(new CreateSalesReturnCommand(
                trip.BranchId, trip.Vehicle.WarehouseId, command.CustomerId, null, command.ReturnedAt,
                $"Trip return {tag}{(string.IsNullOrWhiteSpace(command.Reason) ? "" : " — " + command.Reason)}", null, currentUser.UserId,
                command.Lines.Select(l => new SalesReturnLineInput(l.ItemId, l.Qty, items[l.ItemId].BaseUomId, l.UnitPrice, null, items[l.ItemId].VatType)).ToList(),
                ForceQuickPost: true), cancellationToken);
            if (!created.IsSuccess)
                return Result.Failure<Guid>(created.Error!);
            salesReturnId = created.Value!.Id;
        }

        dbContext.ChangeTracker.Clear();
        var tripToUpdate = await dbContext.Trips.Include(t => t.Lines).FirstAsync(t => t.Id == trip.Id, cancellationToken);
        foreach (var l in command.Lines)
        {
            var line = TripShared.LineFor(tripToUpdate, items[l.ItemId]);
            if (l.IsDamaged) line.QtyReturnedDamaged += l.Qty;
            else line.QtyReturnedGood += l.Qty;
        }
        var done = await dbContext.TripReturns.FirstAsync(r => r.Id == recordId, cancellationToken);
        done.SalesReturnId = salesReturnId;
        done.Status = "COMPLETED";
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(recordId);
    }
}

// ================================================================ Collection of an open balance

/// <summary>
/// One invoice a collection pays: either by its ZARI id (an invoice that was open when the trip was downloaded), or by
/// the handheld number of an invoice issued earlier on this same trip — the device has no ZARI id for that one until
/// its sale syncs, and the outbox always sends the sale first.
/// </summary>
public sealed record TripCollectionAllocation(Guid? SalesInvoiceId, decimal Amount, string? InvoiceNumber = null);

/// <summary>Payment collected on the road against the customer's open invoices. Idempotent on ClientRef.</summary>
public sealed record RecordTripCollectionCommand(Guid TripId, Guid ClientRef, Guid CustomerId, DateTimeOffset CollectedAt,
    List<TripCollectionAllocation> Allocations, List<CustomerPaymentTenderInput> Tenders) : ICommand<Result<Guid>>;

public sealed class RecordTripCollectionValidator : AbstractValidator<RecordTripCollectionCommand>
{
    public RecordTripCollectionValidator()
    {
        RuleFor(x => x.ClientRef).NotEmpty();
        RuleFor(x => x.Allocations).NotEmpty();
        RuleFor(x => x.Tenders).NotEmpty();
        RuleForEach(x => x.Allocations).ChildRules(a =>
        {
            a.RuleFor(x => x.Amount).GreaterThan(0);
            a.RuleFor(x => x).Must(x => x.SalesInvoiceId.HasValue != !string.IsNullOrWhiteSpace(x.InvoiceNumber))
                .WithName("Allocation").WithMessage("Each allocation names an invoice by its id or by its number on this trip, not both.");
        });
        RuleForEach(x => x.Tenders).ChildRules(t => t.RuleFor(x => x.Amount).GreaterThan(0));
        RuleFor(x => x).Must(x => x.Allocations == null || x.Tenders == null || x.Allocations.Sum(a => a.Amount) == x.Tenders.Sum(t => t.Amount))
            .WithName("Tenders").WithMessage("The amounts tendered must equal the amounts applied to invoices.");
    }
}

public sealed class RecordTripCollectionCommandHandler(
    IAppDbContext dbContext, ICurrentUser currentUser, IPermissionService permissionService,
    ICommandHandler<CreateCustomerPaymentCommand, Result<CustomerPaymentResponse>> createCustomerPaymentHandler)
    : ICommandHandler<RecordTripCollectionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(RecordTripCollectionCommand command, CancellationToken cancellationToken = default)
    {
        var trip = await dbContext.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == command.TripId, cancellationToken);
        if (trip is null)
            return Result.Failure<Guid>(Error.NotFound("Trip.NotFound", $"Trip with ID '{command.TripId}' was not found."));
        if (!TripShared.IsDriver(trip, currentUser.UserId))
            return Result.Failure<Guid>(Error.Forbidden("Trip.NotTheDriver", "Only this trip's driver can record its collections."));
        if (!await permissionService.HasPermissionOnBranchAsync("ROUTE_SALES", FormAction.Create, trip.BranchId, cancellationToken))
            return Result.Failure<Guid>(Error.Forbidden("RouteSale.Forbidden", "You do not have permission to record route sales for this branch."));

        var record = await dbContext.TripCollections.FirstOrDefaultAsync(c => c.ClientRef == command.ClientRef, cancellationToken);
        if (record is { Status: "COMPLETED" })
            return Result.Success(record.Id);

        var methodIds = command.Tenders.Select(t => t.PaymentMethodId).Distinct().ToList();
        var methods = await dbContext.PaymentMethods.AsNoTracking().Where(m => methodIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id, cancellationToken);
        if (methods.Count != methodIds.Count)
            return Result.Failure<Guid>(Error.NotFound("PaymentMethod.NotFound", "One or more payment methods were not found."));
        var cash = command.Tenders.Where(t => methods[t.PaymentMethodId].Code == "CASH").Sum(t => t.Amount);

        // Invoices issued on this trip are named by their handheld number; resolve them to ZARI's invoices.
        var numbers = command.Allocations.Where(a => a.SalesInvoiceId is null).Select(a => a.InvoiceNumber!.Trim()).Distinct().ToList();
        var issuedHere = numbers.Count == 0 ? [] : await dbContext.RouteSales.AsNoTracking()
            .Where(r => r.TripId == trip.Id && numbers.Contains(r.BirOrSeriesNumber) && r.Status == "COMPLETED" && r.SalesInvoiceId != null)
            .ToDictionaryAsync(r => r.BirOrSeriesNumber, r => r.SalesInvoiceId!.Value, cancellationToken);
        if (numbers.FirstOrDefault(n => !issuedHere.ContainsKey(n)) is { } unknown)
            return Result.Failure<Guid>(Error.Validation("TripCollection.InvoiceNotOnTrip",
                $"Invoice {unknown} hasn't been recorded on this trip — its sale must reach ZARI before a collection against it."));
        var allocations = command.Allocations
            .Select(a => new CustomerPaymentLineInput(a.SalesInvoiceId ?? issuedHere[a.InvoiceNumber!.Trim()], a.Amount)).ToList();

        if (record is null)
        {
            if (trip.Status != TripStatus.InTransit)
                return Result.Failure<Guid>(Error.Validation("Trip.NotOnTheRoad", "Collections can only be recorded while the trip is on the road."));
            record = new TripCollection
            {
                TripId = trip.Id, ClientRef = command.ClientRef, CustomerId = command.CustomerId, Amount = command.Tenders.Sum(t => t.Amount),
                CashAmount = cash, Status = "PENDING", CollectedAt = command.CollectedAt, CreatedBy = currentUser.UserId,
            };
            dbContext.TripCollections.Add(record);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        var recordId = record.Id;
        var tag = TripShared.Tag("TC", recordId);

        var paymentId = record.CustomerPaymentId ?? await dbContext.CustomerPayments.Where(p => p.Remarks != null && p.Remarks.Contains(tag))
            .Select(p => (Guid?)p.Id).FirstOrDefaultAsync(cancellationToken);
        if (paymentId is null)
        {
            var payment = await createCustomerPaymentHandler.HandleAsync(new CreateCustomerPaymentCommand(
                trip.BranchId, command.CustomerId, command.CollectedAt, $"Trip collection {tag}", null, currentUser.UserId,
                allocations, Tenders: command.Tenders, ForceQuickPost: true), cancellationToken);
            if (!payment.IsSuccess)
                return Result.Failure<Guid>(payment.Error!);
            paymentId = payment.Value!.Id;
        }

        dbContext.ChangeTracker.Clear();
        var tripToUpdate = await dbContext.Trips.FirstAsync(t => t.Id == trip.Id, cancellationToken);
        tripToUpdate.CashExpected += cash;
        var done = await dbContext.TripCollections.FirstAsync(c => c.Id == recordId, cancellationToken);
        done.CustomerPaymentId = paymentId;
        done.Status = "COMPLETED";
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(recordId);
    }
}

// ================================================================ Refused at a stop (nothing accepted)

/// <summary>
/// A stop where the customer accepted none of (part of) their order — no invoice is issued, so no number
/// is used. Each line says why. The goods stay on the truck; the order line stays open for a later trip.
/// (A partial delivery records its refusals with the route sale instead.) Idempotent on ClientRef.
/// </summary>
public sealed record RecordTripRefusalCommand(Guid TripId, Guid ClientRef, Guid CustomerId, Guid SalesOrderId, DateTimeOffset RefusedAt,
    List<TripRefusalLineInput> Lines) : ICommand<Result<Guid>>;

public sealed class RecordTripRefusalValidator : AbstractValidator<RecordTripRefusalCommand>
{
    public RecordTripRefusalValidator()
    {
        RuleFor(x => x.ClientRef).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.SalesOrderId).NotEmpty();
        RuleFor(x => x.RefusedAt).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("List what was refused.");
        RuleForEach(x => x.Lines).ChildRules(TripRefusals.LineRules);
    }
}

public sealed class RecordTripRefusalCommandHandler(IAppDbContext dbContext, ICurrentUser currentUser, IPermissionService permissionService)
    : ICommandHandler<RecordTripRefusalCommand, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(RecordTripRefusalCommand command, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.TripRefusals.AsNoTracking().FirstOrDefaultAsync(r => r.ClientRef == command.ClientRef, cancellationToken);
        if (existing is not null)
            return existing.TripId == command.TripId
                ? Result.Success(existing.Id)
                : Result.Failure<Guid>(Error.Conflict("TripRefusal.ClientRefReused", "That refusal id was already used on another trip."));

        var trip = await dbContext.Trips.AsNoTracking().Include(t => t.SalesOrders).FirstOrDefaultAsync(t => t.Id == command.TripId, cancellationToken);
        if (trip is null)
            return Result.Failure<Guid>(Error.NotFound("Trip.NotFound", $"Trip with ID '{command.TripId}' was not found."));
        if (!TripShared.IsDriver(trip, currentUser.UserId))
            return Result.Failure<Guid>(Error.Forbidden("Trip.NotTheDriver", "Only this trip's driver can record refusals."));
        if (!await permissionService.HasPermissionOnBranchAsync("ROUTE_SALES", FormAction.Create, trip.BranchId, cancellationToken))
            return Result.Failure<Guid>(Error.Forbidden("RouteSale.Forbidden", "You do not have permission to record route sales for this branch."));
        if (trip.Status != TripStatus.InTransit)
            return Result.Failure<Guid>(Error.Validation("RouteSale.TripNotOnTheRoad", "Refusals can only be recorded while the trip is on the road."));
        if (trip.SalesOrders.All(s => s.SalesOrderId != command.SalesOrderId))
            return Result.Failure<Guid>(Error.Validation("RouteSale.SalesOrderNotOnTrip", "That sales order isn't assigned to this trip."));

        var order = await dbContext.SalesOrders.AsNoTracking().Include(o => o.Lines).ThenInclude(l => l.Item)
            .FirstAsync(o => o.Id == command.SalesOrderId, cancellationToken);
        if (order.CustomerId != command.CustomerId)
            return Result.Failure<Guid>(Error.Validation("RouteSale.SalesOrderOtherCustomer", $"Sales order {order.SoNo} belongs to another customer."));
        if (await TripRefusals.CheckAsync(dbContext, trip.Id, order, new Dictionary<Guid, decimal>(), command.Lines, requireEveryLine: false, cancellationToken) is { } error)
            return Result.Failure<Guid>(error);

        var tripToUpdate = await dbContext.Trips.Include(t => t.Lines).FirstAsync(t => t.Id == trip.Id, cancellationToken);
        var refusal = TripRefusals.Build(trip.Id, command.ClientRef, command.CustomerId, order.Id, null, command.RefusedAt, command.Lines,
            order.Lines.ToDictionary(l => l.Id, l => l.ItemId), currentUser.UserId);
        dbContext.TripRefusals.Add(refusal);
        foreach (var l in command.Lines)
            TripShared.LineFor(tripToUpdate, order.Lines.First(x => x.Id == l.SalesOrderLineId).Item).QtyRefused += l.Qty;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The same refusal arriving twice at once — the other request recorded it.
            dbContext.ChangeTracker.Clear();
            var recorded = await dbContext.TripRefusals.AsNoTracking().FirstOrDefaultAsync(r => r.ClientRef == command.ClientRef, cancellationToken);
            if (recorded is null) throw;
            return Result.Success(recorded.Id);
        }
        return Result.Success(refusal.Id);
    }
}

// ================================================================ Proof of delivery

/// <summary>Attaches who received the goods and the uploaded signature/photo to a recorded sale.</summary>
public sealed record SetRouteSalePodCommand(Guid RouteSaleId, string? ReceivedByName, string? SignatureFilePath, string? PhotoFilePath) : ICommand;

public sealed class SetRouteSalePodCommandHandler(IAppDbContext dbContext, ICurrentUser currentUser) : ICommandHandler<SetRouteSalePodCommand, Result>
{
    public async Task<Result> HandleAsync(SetRouteSalePodCommand command, CancellationToken cancellationToken = default)
    {
        var sale = await dbContext.RouteSales.Include(r => r.Trip).FirstOrDefaultAsync(r => r.Id == command.RouteSaleId, cancellationToken);
        if (sale is null)
            return Result.Failure(Error.NotFound("RouteSale.NotFound", "The route sale was not found."));
        if (!TripShared.IsDriver(sale.Trip, currentUser.UserId))
            return Result.Failure(Error.Forbidden("RouteSale.NotTheDriver", "Only this trip's driver can attach proof of delivery."));

        if (command.ReceivedByName is not null) sale.ReceivedByName = command.ReceivedByName;
        if (command.SignatureFilePath is not null) sale.SignatureFilePath = command.SignatureFilePath;
        if (command.PhotoFilePath is not null) sale.PhotoFilePath = command.PhotoFilePath;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ================================================================ End trip: IN_TRANSIT -> PENDING_CLOSE_APPROVAL

/// <summary>The driver's end-of-trip count of good stock left on the truck, and the cash handed in.</summary>
public sealed record EndTripCommand(Guid TripId, decimal CashRemitted, List<TripCountInput> Counts) : ICommand<Result<TripResponse>>;

public sealed class EndTripCommandHandler(
    IAppDbContext dbContext, ICurrentUser currentUser, IPermissionService permissionService,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler)
    : ICommandHandler<EndTripCommand, Result<TripResponse>>
{
    public async Task<Result<TripResponse>> HandleAsync(EndTripCommand command, CancellationToken cancellationToken = default)
    {
        var trip = await dbContext.Trips.Include(t => t.Lines).ThenInclude(l => l.Item).FirstOrDefaultAsync(t => t.Id == command.TripId, cancellationToken);
        if (trip is null)
            return Result.Failure<TripResponse>(Error.NotFound("Trip.NotFound", $"Trip with ID '{command.TripId}' was not found."));
        if (!TripShared.IsDriver(trip, currentUser.UserId)
            && !await permissionService.HasPermissionOnBranchAsync("TRIPS", FormAction.Edit, trip.BranchId, cancellationToken))
            return Result.Failure<TripResponse>(Error.Forbidden("Trip.NotTheDriver", "Only this trip's driver (or the branch) can end the trip."));
        if (trip.Status != TripStatus.InTransit)
            return Result.Failure<TripResponse>(Error.Validation("Trip.NotOnTheRoad", "Only a trip on the road can be ended."));
        if (command.CashRemitted < 0)
            return Result.Failure<TripResponse>(Error.Validation("Trip.InvalidCash", "Cash remitted can't be negative."));

        var pending = await dbContext.RouteSales.Where(r => r.TripId == trip.Id && r.Status == "PENDING").Select(r => r.BirOrSeriesNumber).ToListAsync(cancellationToken);
        if (pending.Count > 0)
            return Result.Failure<TripResponse>(Error.Validation("Trip.UnfinishedSales",
                $"These invoices were not fully recorded yet: {string.Join(", ", pending)}. Sync them (or void them) before ending the trip."));

        var counts = command.Counts.GroupBy(c => c.ItemId).ToDictionary(g => g.Key, g => g.Sum(c => c.QtyCounted));
        var missing = trip.Lines.Where(l => TripShared.ExpectedGoodOnTruck(l) != 0 && !counts.ContainsKey(l.ItemId)).Select(l => l.Item.Code).ToList();
        if (missing.Count > 0)
            return Result.Failure<TripResponse>(Error.Validation("Trip.CountMissing", $"Count every item still on the truck: {string.Join(", ", missing)}."));
        if (counts.Values.Any(q => q < 0))
            return Result.Failure<TripResponse>(Error.Validation("Trip.InvalidCount", "Counts can't be negative."));

        foreach (var line in trip.Lines)
            line.QtyCountedAtEnd = counts.GetValueOrDefault(line.ItemId);
        trip.CashRemitted = command.CashRemitted;
        trip.EndedAt = DateTimeOffset.UtcNow;

        var submit = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand(TripApproval.CloseEntity, trip.Id.ToString(), trip.BranchId, currentUser.UserId ?? "driver", null, null), cancellationToken);
        if (!submit.IsSuccess)
            return Result.Failure<TripResponse>(submit.Error!);

        trip.Status = TripStatus.PendingCloseApproval;
        await dbContext.SaveChangesAsync(cancellationToken);
        await createNotificationHandler.HandleAsync(new CreateNotificationCommand("TRIP", trip.Id.ToString(), trip.BranchId, "ENDED",
            "APPROVAL_NEEDED", "ended this trip — counts and cash are ready for the close", currentUser.UserId), cancellationToken);
        return Result.Success(await TripShared.LoadResponseAsync(dbContext, trip.Id, cancellationToken));
    }
}
