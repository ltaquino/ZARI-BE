namespace ZARI.Application.Features.RouteSales.Trips.Close;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Inventory.StockLedgers.Issue;
using ZARI.Application.Features.Inventory.StockLedgers.Receive;
using ZARI.Application.Features.RouteSales.Trips.LoadWorkflow;
using ZARI.Application.Features.RouteSales.Trips.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// The branch manager closes a trip the driver has ended (RouteSalesContext.md §4):
///   1. stock variance: counted − expected good stock per item → SHORTAGE (out of the truck) or
///      OVERAGE (into it), valued at cost;
///   2. LOAD_IN: the counted good stock back to the trip's source warehouse;
///   3. DAMAGED: damaged customer returns from the truck to the branch's Damaged warehouse;
///   4. one GL journal for the stock variance and the cash variance (expected vs remitted);
///   5. shortages (stock at cost + short cash) settled as the manager chooses (TripShortageSettlement):
///      PAYROLL — Dr Due from Employees + a one-payroll EmployeeDeduction (driver must be linked to HR);
///      DRIVER_PAYS — Dr Due from Employees, cleared as the driver pays in (TripShortagePayment);
///      WRITE_OFF — Dr Inventory Variance / Other Operating Expense. Default: PAYROLL when the driver is
///      linked to an HR employee, else DRIVER_PAYS — unless the company has payroll deduction of shortages
///      turned off (Company.RouteShortagePayrollDeductionEnabled), when PAYROLL isn't allowed and the default
///      is WRITE_OFF: the shortage is only recorded (Trip.ShortageAmount) and reported.
/// The truck ends empty. Resumable: moves post idempotently, the journal and deduction are each
/// guarded, and the trip lines/status are written in one final save.
///
/// The truck's BIR Z-Reading is deliberately NOT run here: a truck can run several trips a day and
/// the Z-Reading is the end-of-day close — run it from POS Closing with the truck's terminal.
/// </summary>
public sealed record ApproveTripCloseCommand(Guid Id, string ApproverUserId, string? Comments, string? ShortageSettlement = null)
    : ICommand<Result<TripResponse>>;

public sealed class ApproveTripCloseCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<IssueStockLinesCommand, Result<IssueStockLinesResponse>> issueHandler,
    ICommandHandler<ReceiveStockCommand, Result<ReceiveStockResponse>> receiveHandler,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService) : ICommandHandler<ApproveTripCloseCommand, Result<TripResponse>>
{
    public const string JournalTable = "TripClose";

    private sealed record LinePlan(Guid ItemId, decimal ExpectedGood, decimal Counted, decimal Variance, decimal DamagedOnTruck);

    public async Task<Result<TripResponse>> HandleAsync(ApproveTripCloseCommand command, CancellationToken cancellationToken = default)
    {
        var trip = await dbContext.Trips.AsNoTracking().Include(t => t.Lines).ThenInclude(l => l.Item).Include(t => t.Vehicle)
            .FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);
        if (trip is null)
            return Fail(Error.NotFound("Trip.NotFound", $"Trip with ID '{command.Id}' was not found."));
        if (!await permissionService.HasPermissionOnBranchAsync("TRIPS", FormAction.Approve, trip.BranchId, cancellationToken))
            return Fail(Error.Forbidden("Trip.Forbidden", "You do not have permission to close trips for this branch."));
        if (trip.Status != TripStatus.PendingCloseApproval)
            return Fail(Error.Validation("Trip.NotPendingClose", "Only a trip the driver has ended can be closed."));

        var plan = trip.Lines.Select(l =>
        {
            var expected = TripShared.ExpectedGoodOnTruck(l);
            var counted = l.QtyCountedAtEnd ?? 0;
            return new LinePlan(l.ItemId, expected, counted, counted - expected, l.QtyReturnedDamaged - l.QtyDamagedOut);
        }).ToList();

        Warehouse? damagedWarehouse = null;
        if (plan.Any(p => p.DamagedOnTruck > 0))
        {
            damagedWarehouse = await dbContext.Warehouses.AsNoTracking()
                .Where(w => w.BranchId == trip.BranchId && w.WarehouseType == "Damaged" && w.Status == "active")
                .OrderBy(w => w.Code).FirstOrDefaultAsync(cancellationToken);
            if (damagedWarehouse is null)
                return Fail(Error.Validation("Trip.NoDamagedWarehouse",
                    "Damaged returns are on the truck but this branch has no active Damaged warehouse — create one (type Damaged) first."));
        }

        // The settlement is fixed by the first attempt that got this far, so a retried close can't book it two ways.
        var payrollAllowed = await dbContext.Companies.Select(co => (bool?)co.RouteShortagePayrollDeductionEnabled).FirstOrDefaultAsync(cancellationToken) ?? true;
        var settlement = trip.ShortageSettlement
            ?? command.ShortageSettlement?.Trim().ToUpperInvariant()
            ?? (!payrollAllowed ? TripShortageSettlement.WriteOff
                : trip.DriverEmployeeId is not null ? TripShortageSettlement.Payroll : TripShortageSettlement.DriverPays);
        if (!TripShortageSettlement.All.Contains(settlement))
            return Fail(Error.Validation("Trip.InvalidShortageSettlement", "Shortage settlement must be PAYROLL, DRIVER_PAYS or WRITE_OFF."));
        if (settlement == TripShortageSettlement.Payroll && !payrollAllowed && trip.ShortageSettlement is null)
            return Fail(Error.Validation("Trip.PayrollDeductionDisabled",
                "Payroll deduction of trip shortages is turned off in Company Settings — choose WRITE_OFF (report only) or DRIVER_PAYS."));
        if (settlement == TripShortageSettlement.Payroll && trip.DriverEmployeeId is null)
            return Fail(Error.Validation("Trip.DriverNotLinkedToHr",
                "The driver isn't linked to an HR employee, so a shortage can't go through payroll — choose DRIVER_PAYS (the driver pays it in) or WRITE_OFF."));

        var request = await TripApproval.LatestAsync(dbContext, TripApproval.CloseEntity, trip.Id, cancellationToken);
        if (request is null)
            return Fail(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this trip's close."));
        if (request.Status == "PENDING")
        {
            var decide = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
            if (!decide.IsSuccess)
                return Fail(decide.Error!);
        }

        if (trip.ShortageSettlement is null)
        {
            var choosing = await dbContext.Trips.FirstAsync(t => t.Id == trip.Id, cancellationToken);
            choosing.ShortageSettlement = settlement;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // ---- 1-3. Stock moves (created once, posted idempotently, in this order so the truck ends at zero).
        var existingMoves = await dbContext.TripStockMoves.AsNoTracking().Include(m => m.Lines).Where(m => m.TripId == trip.Id).ToListAsync(cancellationToken);
        var loadOutCost = existingMoves.FirstOrDefault(m => m.MoveType == TripMoveType.LoadOut)?.Lines
            .GroupBy(l => l.ItemId).ToDictionary(g => g.Key, g => g.First().UnitCost) ?? [];
        var truck = trip.Vehicle.WarehouseId;

        var wanted = new List<TripStockMove>
        {
            Move(trip.Id, TripMoveType.Shortage, truck, null, plan.Where(p => p.Variance < 0).Select(p => (p.ItemId, -p.Variance, 0m))),
            Move(trip.Id, TripMoveType.Overage, null, truck, plan.Where(p => p.Variance > 0).Select(p => (p.ItemId, p.Variance, loadOutCost.GetValueOrDefault(p.ItemId)))),
            Move(trip.Id, TripMoveType.LoadIn, truck, trip.SourceWarehouseId, plan.Where(p => p.Counted > 0).Select(p => (p.ItemId, p.Counted, 0m))),
            Move(trip.Id, TripMoveType.Damaged, truck, damagedWarehouse?.Id, plan.Where(p => p.DamagedOnTruck > 0).Select(p => (p.ItemId, p.DamagedOnTruck, 0m))),
        };
        var toCreate = wanted.Where(w => w.Lines.Count > 0 && existingMoves.All(e => e.MoveType != w.MoveType)).ToList();
        if (toCreate.Count > 0)
        {
            dbContext.TripStockMoves.AddRange(toCreate);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        foreach (var type in new[] { TripMoveType.Shortage, TripMoveType.Overage, TripMoveType.LoadIn, TripMoveType.Damaged })
        {
            var moveId = await dbContext.TripStockMoves.Where(m => m.TripId == trip.Id && m.MoveType == type).Select(m => (Guid?)m.Id).FirstOrDefaultAsync(cancellationToken);
            if (moveId is null) continue;
            var posted = await TripStockMovePostingService.PostAsync(dbContext, issueHandler, receiveHandler, moveId.Value, trip.BranchId, DateTimeOffset.UtcNow, cancellationToken);
            if (!posted.IsSuccess)
                return Fail(posted.Error!);
        }
        dbContext.ChangeTracker.Clear();

        // ---- 4. One journal for stock + cash variance.
        var moves = await dbContext.TripStockMoves.AsNoTracking().Include(m => m.Lines).ThenInclude(l => l.Item)
            .Where(m => m.TripId == trip.Id).ToListAsync(cancellationToken);
        var shortageLines = moves.FirstOrDefault(m => m.MoveType == TripMoveType.Shortage)?.Lines ?? [];
        var overageLines = moves.FirstOrDefault(m => m.MoveType == TripMoveType.Overage)?.Lines ?? [];
        var shortageValue = Math.Round(shortageLines.Sum(l => l.Qty * l.UnitCost), 4);
        var cashShort = Math.Max(0, (trip.CashExpected) - (trip.CashRemitted ?? 0));
        var cashOver = Math.Max(0, (trip.CashRemitted ?? 0) - trip.CashExpected);
        var charge = settlement != TripShortageSettlement.WriteOff;

        if (!await dbContext.GlJournals.AnyAsync(j => j.SourceReferenceTable == JournalTable && j.SourceReferenceId == trip.Id.ToString(), cancellationToken))
        {
            var journal = await BuildJournalAsync(trip, shortageLines, overageLines, cashShort, cashOver, charge, cancellationToken);
            if (!journal.IsSuccess)
                return Fail(journal.Error!);
            if (journal.Value!.Count > 0)
            {
                var post = await postGlJournalHandler.HandleAsync(new PostGlJournalCommand(trip.BranchId, DateTimeOffset.UtcNow, "ROUTE_SALES",
                    JournalTable, trip.Id.ToString(), $"Trip {trip.TripNo} close — stock and cash variance", journal.Value), cancellationToken);
                if (!post.IsSuccess)
                    return Fail(post.Error!);
            }
        }

        // ---- 5. Charge the driver once (payroll: a deduction; driver pays: the trip carries the balance).
        var chargeAmount = charge ? Math.Round(shortageValue + cashShort, 4) : 0;
        var closedAt = DateTimeOffset.UtcNow;
        dbContext.ChangeTracker.Clear();
        var closing = await dbContext.Trips.Include(t => t.Lines).FirstAsync(t => t.Id == trip.Id, cancellationToken);
        if (chargeAmount > 0 && settlement == TripShortageSettlement.Payroll && closing.EmployeeDeductionId is null)
        {
            var deduction = new EmployeeDeduction
            {
                EmployeeId = trip.DriverEmployeeId!.Value,
                Code = EmployeeDeduction.TripShortageCode,
                Name = $"Trip {trip.TripNo} shortage",
                Amount = chargeAmount,
                // One payroll: the period that contains the close date.
                EffectiveDate = closedAt,
                EndDate = closedAt,
                Status = "active",
                CreatedBy = command.ApproverUserId,
            };
            dbContext.EmployeeDeductions.Add(deduction);
            closing.EmployeeDeductionId = deduction.Id; // EF assigns the key on Add
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // ---- Final save: trip quantities and status.
        foreach (var line in closing.Lines)
        {
            var p = plan.First(x => x.ItemId == line.ItemId);
            line.QtyVariance = p.Variance;
            line.QtyLoadedBack = p.Counted;
            line.QtyDamagedOut += p.DamagedOnTruck;
        }
        closing.ShortageAmount = Math.Round(shortageValue + cashShort, 4);
        closing.ShortageCharged = chargeAmount;
        // Recorded only when there was something to settle.
        closing.ShortageSettlement = shortageValue + cashShort > 0 ? settlement : null;
        closing.Status = TripStatus.Closed;
        closing.ClosedAt = closedAt;
        await dbContext.SaveChangesAsync(cancellationToken);

        await createNotificationHandler.HandleAsync(new CreateNotificationCommand("TRIP", trip.Id.ToString(), trip.BranchId, "CLOSED", "ACTIVITY",
            settlement switch
            {
                _ when shortageValue + cashShort <= 0 => "closed this trip",
                _ when chargeAmount <= 0 => $"closed this trip — {shortageValue + cashShort:N2} shortage recorded (not charged)",
                TripShortageSettlement.Payroll => $"closed this trip — {chargeAmount:N2} shortage charged to the driver's payroll",
                _ => $"closed this trip — the driver owes {chargeAmount:N2} for the shortage",
            }, command.ApproverUserId), cancellationToken);
        return Result.Success(await TripShared.LoadResponseAsync(dbContext, trip.Id, cancellationToken));
    }

    private static TripStockMove Move(Guid tripId, string type, Guid? from, Guid? to, IEnumerable<(Guid ItemId, decimal Qty, decimal Cost)> lines) => new()
    {
        TripId = tripId,
        MoveType = type,
        FromWarehouseId = from,
        ToWarehouseId = to,
        Status = "PENDING",
        Lines = lines.Select(l => new TripStockMoveLine { ItemId = l.ItemId, Qty = l.Qty, UnitCost = l.Cost }).ToList(),
    };

    private async Task<Result<List<PostGlJournalLineInput>>> BuildJournalAsync(Trip trip, List<TripStockMoveLine> shortage, List<TripStockMoveLine> overage,
        decimal cashShort, decimal cashOver, bool charge, CancellationToken ct)
    {
        var lines = new List<PostGlJournalLineInput>();

        async Task<Result<Guid>> InventoryAccount(Item item) => Guid.TryParse(item.InventoryAccountId, out var explicitId)
            ? Result.Success(explicitId)
            : await TripShared.AccountIdAsync(dbContext, "1400", "Inventory Asset", ct);

        if (shortage.Count > 0 || overage.Count > 0)
        {
            var variance = await TripShared.AccountIdAsync(dbContext, "5100", "Inventory Variance / Shrinkage", ct);
            if (!variance.IsSuccess) return Result.Failure<List<PostGlJournalLineInput>>(variance.Error!);
            var shortageDebit = variance;
            if (charge && shortage.Count > 0)
            {
                shortageDebit = await TripShared.AccountIdAsync(dbContext, "1220", "Due from Employees", ct);
                if (!shortageDebit.IsSuccess) return Result.Failure<List<PostGlJournalLineInput>>(shortageDebit.Error!);
            }
            foreach (var l in shortage)
            {
                var amount = Math.Round(l.Qty * l.UnitCost, 4);
                if (amount <= 0) continue;
                var inventory = await InventoryAccount(l.Item);
                if (!inventory.IsSuccess) return Result.Failure<List<PostGlJournalLineInput>>(inventory.Error!);
                lines.Add(new PostGlJournalLineInput(shortageDebit.Value, null, amount, 0, null));
                lines.Add(new PostGlJournalLineInput(inventory.Value, null, 0, amount, null));
            }
            foreach (var l in overage)
            {
                var amount = Math.Round(l.Qty * l.UnitCost, 4);
                if (amount <= 0) continue;
                var inventory = await InventoryAccount(l.Item);
                if (!inventory.IsSuccess) return Result.Failure<List<PostGlJournalLineInput>>(inventory.Error!);
                lines.Add(new PostGlJournalLineInput(inventory.Value, null, amount, 0, null));
                lines.Add(new PostGlJournalLineInput(variance.Value, null, 0, amount, null));
            }
        }

        if (cashShort > 0 || cashOver > 0)
        {
            // Cash tenders post to the CASH payment method's own account; fall back to Cash on Hand.
            var cashAccount = await dbContext.PaymentMethods.Where(m => m.Code == "CASH").Select(m => (Guid?)m.GlAccountId).FirstOrDefaultAsync(ct)
                is { } cashId ? Result.Success(cashId) : await TripShared.AccountIdAsync(dbContext, "1000", "Cash on Hand", ct);
            if (!cashAccount.IsSuccess) return Result.Failure<List<PostGlJournalLineInput>>(cashAccount.Error!);
            var shortOver = await TripShared.AccountIdAsync(dbContext, "6900", "Other Operating Expense", ct);
            if (!shortOver.IsSuccess) return Result.Failure<List<PostGlJournalLineInput>>(shortOver.Error!);
            if (cashShort > 0)
            {
                var debit = shortOver;
                if (charge)
                {
                    debit = await TripShared.AccountIdAsync(dbContext, "1220", "Due from Employees", ct);
                    if (!debit.IsSuccess) return Result.Failure<List<PostGlJournalLineInput>>(debit.Error!);
                }
                lines.Add(new PostGlJournalLineInput(debit.Value, null, cashShort, 0, null));
                lines.Add(new PostGlJournalLineInput(cashAccount.Value, null, 0, cashShort, null));
            }
            if (cashOver > 0)
            {
                lines.Add(new PostGlJournalLineInput(cashAccount.Value, null, cashOver, 0, null));
                lines.Add(new PostGlJournalLineInput(shortOver.Value, null, 0, cashOver, null));
            }
        }

        return Result.Success(lines);
    }

    private static Result<TripResponse> Fail(Error error) => Result.Failure<TripResponse>(error);
}

/// <summary>Sends an ended trip back to the driver (IN_TRANSIT) to recount or fix something.</summary>
public sealed record RejectTripCloseCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<TripResponse>>;

public sealed class RejectTripCloseCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService) : ICommandHandler<RejectTripCloseCommand, Result<TripResponse>>
{
    public async Task<Result<TripResponse>> HandleAsync(RejectTripCloseCommand command, CancellationToken cancellationToken = default)
    {
        var trip = await dbContext.Trips.FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);
        if (trip is null)
            return Result.Failure<TripResponse>(Error.NotFound("Trip.NotFound", $"Trip with ID '{command.Id}' was not found."));
        if (!await permissionService.HasPermissionOnBranchAsync("TRIPS", FormAction.Approve, trip.BranchId, cancellationToken))
            return Result.Failure<TripResponse>(Error.Forbidden("Trip.Forbidden", "You do not have permission to close trips for this branch."));
        if (trip.Status != TripStatus.PendingCloseApproval)
            return Result.Failure<TripResponse>(Error.Validation("Trip.NotPendingClose", "Only a trip waiting for its close can be sent back."));

        if (await TripApproval.LatestAsync(dbContext, TripApproval.CloseEntity, trip.Id, cancellationToken) is { Status: "PENDING" } request)
        {
            var decide = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Reject", command.Comments), cancellationToken);
            if (!decide.IsSuccess)
                return Result.Failure<TripResponse>(decide.Error!);
        }

        trip.Status = TripStatus.InTransit;
        trip.EndedAt = null;
        trip.ShortageSettlement = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        await createNotificationHandler.HandleAsync(new CreateNotificationCommand("TRIP", trip.Id.ToString(), trip.BranchId, "REJECTED",
            "ACTIVITY", TripShared.NotificationMessage($"sent this trip back to the driver to recount: {command.Comments}"), command.ApproverUserId), cancellationToken);
        return Result.Success(await TripShared.LoadResponseAsync(dbContext, trip.Id, cancellationToken));
    }
}
