namespace ZARI.Application.Features.RouteSales.Trips.LoadWorkflow;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockLedgers.Issue;
using ZARI.Application.Features.Inventory.StockLedgers.Receive;
using ZARI.Application.Features.RouteSales.Trips.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

internal static class TripApproval
{
    public const string LoadEntity = "TRIP_LOAD";
    public const string CloseEntity = "TRIP_CLOSE";

    public static Task<ApprovalRequest?> LatestAsync(IAppDbContext dbContext, string entityType, Guid tripId, CancellationToken ct) =>
        dbContext.ApprovalRequests.Where(r => r.EntityType == entityType && r.EntityId == tripId.ToString())
            .OrderByDescending(r => r.RequestedAt).FirstOrDefaultAsync(ct);
}

// ---------------------------------------------------------------- Delete (DRAFT only)

public sealed record DeleteTripCommand(Guid Id) : ICommand;

public sealed class DeleteTripCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteTripCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteTripCommand command, CancellationToken cancellationToken = default)
    {
        var trip = await dbContext.Trips.FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);
        if (trip is null)
            return Result.Failure(Error.NotFound("Trip.NotFound", $"Trip with ID '{command.Id}' was not found."));
        if (!await permissionService.HasPermissionOnBranchAsync("TRIPS", FormAction.Delete, trip.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("Trip.Forbidden", "You do not have permission to delete trips for this branch."));
        if (trip.Status != TripStatus.Draft)
            return Result.Failure(Error.Validation("Trip.NotDraft", "Only a draft trip can be deleted — cancel it instead."));

        dbContext.Trips.Remove(trip);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ---------------------------------------------------------------- Submit: DRAFT -> PENDING_LOAD_APPROVAL

public sealed record SubmitTripCommand(Guid Id, string RequestedBy) : ICommand<Result<TripResponse>>;

public sealed class SubmitTripCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService) : ICommandHandler<SubmitTripCommand, Result<TripResponse>>
{
    public async Task<Result<TripResponse>> HandleAsync(SubmitTripCommand command, CancellationToken cancellationToken = default)
    {
        var trip = await dbContext.Trips.Include(t => t.Lines).FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);
        if (trip is null)
            return Result.Failure<TripResponse>(Error.NotFound("Trip.NotFound", $"Trip with ID '{command.Id}' was not found."));
        if (!await permissionService.HasPermissionOnBranchAsync("TRIPS", FormAction.Edit, trip.BranchId, cancellationToken))
            return Result.Failure<TripResponse>(Error.Forbidden("Trip.Forbidden", "You do not have permission to update trips for this branch."));
        if (trip.Status != TripStatus.Draft)
            return Result.Failure<TripResponse>(Error.Validation("Trip.NotDraft", "Only a draft trip can be submitted for load approval."));
        if (!trip.Lines.Any(l => l.QtyToLoad > 0))
            return Result.Failure<TripResponse>(Error.Validation("Trip.NothingToLoad", "There is nothing to load on this trip."));

        var submit = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand(TripApproval.LoadEntity, trip.Id.ToString(), trip.BranchId, command.RequestedBy, null, null), cancellationToken);
        if (!submit.IsSuccess)
            return Result.Failure<TripResponse>(submit.Error!);

        trip.Status = TripStatus.PendingLoadApproval;
        await dbContext.SaveChangesAsync(cancellationToken);

        await createNotificationHandler.HandleAsync(new CreateNotificationCommand("TRIP", trip.Id.ToString(), trip.BranchId, "SUBMITTED",
            "APPROVAL_NEEDED", "submitted this trip's load-out for approval", command.RequestedBy), cancellationToken);
        return Result.Success(await TripShared.LoadResponseAsync(dbContext, trip.Id, cancellationToken));
    }
}

// ---------------------------------------------------------------- Approve load: PENDING_LOAD_APPROVAL -> LOADED

/// <summary>
/// The branch manager approves the load-out: the trip's planned quantities move from the source
/// warehouse onto the truck (TripStockMove LOAD_OUT). Resumable like every other ZARI approve: the
/// decision is taken first, and a retry after a failed post skips straight to (idempotent) posting.
/// </summary>
public sealed record ApproveTripLoadCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<TripResponse>>;

public sealed class ApproveTripLoadCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<IssueStockLinesCommand, Result<IssueStockLinesResponse>> issueHandler,
    ICommandHandler<ReceiveStockCommand, Result<ReceiveStockResponse>> receiveHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService) : ICommandHandler<ApproveTripLoadCommand, Result<TripResponse>>
{
    public async Task<Result<TripResponse>> HandleAsync(ApproveTripLoadCommand command, CancellationToken cancellationToken = default)
    {
        var trip = await dbContext.Trips.Include(t => t.Lines).Include(t => t.Vehicle).FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);
        if (trip is null)
            return Result.Failure<TripResponse>(Error.NotFound("Trip.NotFound", $"Trip with ID '{command.Id}' was not found."));
        if (!await permissionService.HasPermissionOnBranchAsync("TRIPS", FormAction.Approve, trip.BranchId, cancellationToken))
            return Result.Failure<TripResponse>(Error.Forbidden("Trip.Forbidden", "You do not have permission to approve trips for this branch."));
        if (trip.Status != TripStatus.PendingLoadApproval)
            return Result.Failure<TripResponse>(Error.Validation("Trip.NotPendingLoadApproval", "Only a trip waiting for load approval can be approved."));

        var busy = await dbContext.Trips.Where(t => t.Id != trip.Id && t.VehicleId == trip.VehicleId && TripStatus.OnTheRoad.Contains(t.Status))
            .Select(t => t.TripNo).FirstOrDefaultAsync(cancellationToken);
        if (busy is not null)
            return Result.Failure<TripResponse>(Error.Conflict("Trip.VehicleBusy", $"This truck is still out on trip {busy} — close that trip first."));

        var request = await TripApproval.LatestAsync(dbContext, TripApproval.LoadEntity, trip.Id, cancellationToken);
        if (request is null)
            return Result.Failure<TripResponse>(Error.NotFound("ApprovalRequest.NotFound", "No approval request found for this trip's load-out."));
        if (request.Status == "PENDING")
        {
            var decide = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Approve", command.Comments), cancellationToken);
            if (!decide.IsSuccess)
                return Result.Failure<TripResponse>(decide.Error!);
        }

        var move = await dbContext.TripStockMoves.FirstOrDefaultAsync(m => m.TripId == trip.Id && m.MoveType == TripMoveType.LoadOut, cancellationToken);
        if (move is null)
        {
            move = new TripStockMove
            {
                TripId = trip.Id,
                MoveType = TripMoveType.LoadOut,
                FromWarehouseId = trip.SourceWarehouseId,
                ToWarehouseId = trip.Vehicle.WarehouseId,
                Status = "PENDING",
                Lines = trip.Lines.Where(l => l.QtyToLoad > 0).Select(l => new TripStockMoveLine { ItemId = l.ItemId, Qty = l.QtyToLoad }).ToList(),
            };
            dbContext.TripStockMoves.Add(move);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var tripId = trip.Id;
        var post = await TripStockMovePostingService.PostAsync(dbContext, issueHandler, receiveHandler, move.Id, trip.BranchId, DateTimeOffset.UtcNow, cancellationToken);
        if (!post.IsSuccess)
            return Result.Failure<TripResponse>(post.Error!);

        // Reload: the stock engines cleared the change tracker.
        dbContext.ChangeTracker.Clear();
        var posted = await dbContext.TripStockMoves.AsNoTracking().Include(m => m.Lines).FirstAsync(m => m.Id == move.Id, cancellationToken);
        var reloaded = await dbContext.Trips.Include(t => t.Lines).FirstAsync(t => t.Id == tripId, cancellationToken);
        foreach (var line in reloaded.Lines)
            line.QtyLoaded = posted.Lines.Where(m => m.ItemId == line.ItemId).Sum(m => m.Qty);
        reloaded.Status = TripStatus.Loaded;
        await dbContext.SaveChangesAsync(cancellationToken);

        await createNotificationHandler.HandleAsync(new CreateNotificationCommand("TRIP", tripId.ToString(), reloaded.BranchId, "APPROVED",
            "ACTIVITY", "approved this trip's load-out — stock moved onto the truck", command.ApproverUserId), cancellationToken);
        return Result.Success(await TripShared.LoadResponseAsync(dbContext, tripId, cancellationToken));
    }
}

// ---------------------------------------------------------------- Reject load: back to DRAFT

public sealed record RejectTripLoadCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<TripResponse>>;

public sealed class RejectTripLoadCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService) : ICommandHandler<RejectTripLoadCommand, Result<TripResponse>>
{
    public async Task<Result<TripResponse>> HandleAsync(RejectTripLoadCommand command, CancellationToken cancellationToken = default)
    {
        var trip = await dbContext.Trips.FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);
        if (trip is null)
            return Result.Failure<TripResponse>(Error.NotFound("Trip.NotFound", $"Trip with ID '{command.Id}' was not found."));
        if (!await permissionService.HasPermissionOnBranchAsync("TRIPS", FormAction.Approve, trip.BranchId, cancellationToken))
            return Result.Failure<TripResponse>(Error.Forbidden("Trip.Forbidden", "You do not have permission to approve trips for this branch."));
        if (trip.Status != TripStatus.PendingLoadApproval)
            return Result.Failure<TripResponse>(Error.Validation("Trip.NotPendingLoadApproval", "Only a trip waiting for load approval can be rejected."));

        var request = await TripApproval.LatestAsync(dbContext, TripApproval.LoadEntity, trip.Id, cancellationToken);
        if (request is { Status: "PENDING" })
        {
            var decide = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.ApproverUserId, "Reject", command.Comments), cancellationToken);
            if (!decide.IsSuccess)
                return Result.Failure<TripResponse>(decide.Error!);
        }

        trip.Status = TripStatus.Draft;
        await dbContext.SaveChangesAsync(cancellationToken);
        await createNotificationHandler.HandleAsync(new CreateNotificationCommand("TRIP", trip.Id.ToString(), trip.BranchId, "REJECTED",
            "ACTIVITY", TripShared.NotificationMessage($"sent this trip's load-out back for re-planning: {command.Comments}"), command.ApproverUserId), cancellationToken);
        return Result.Success(await TripShared.LoadResponseAsync(dbContext, trip.Id, cancellationToken));
    }
}

// ---------------------------------------------------------------- Cancel: before anything has moved

public sealed record CancelTripCommand(Guid Id, string CancelledBy, string Reason) : ICommand<Result<TripResponse>>;

public sealed class CancelTripCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>> decideHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService) : ICommandHandler<CancelTripCommand, Result<TripResponse>>
{
    public async Task<Result<TripResponse>> HandleAsync(CancelTripCommand command, CancellationToken cancellationToken = default)
    {
        var trip = await dbContext.Trips.FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);
        if (trip is null)
            return Result.Failure<TripResponse>(Error.NotFound("Trip.NotFound", $"Trip with ID '{command.Id}' was not found."));
        if (!await permissionService.HasPermissionOnBranchAsync("TRIPS", FormAction.Cancel, trip.BranchId, cancellationToken))
            return Result.Failure<TripResponse>(Error.Forbidden("Trip.Forbidden", "You do not have permission to cancel trips for this branch."));
        if (trip.Status is not (TripStatus.Draft or TripStatus.PendingLoadApproval))
            return Result.Failure<TripResponse>(Error.Validation("Trip.CannotCancel",
                "Only a trip that hasn't been loaded can be cancelled — a loaded trip is ended and closed instead, which brings the stock back."));
        if (string.IsNullOrWhiteSpace(command.Reason))
            return Result.Failure<TripResponse>(Error.Validation("Trip.ReasonRequired", "A reason is required to cancel a trip."));

        if (trip.Status == TripStatus.PendingLoadApproval
            && await TripApproval.LatestAsync(dbContext, TripApproval.LoadEntity, trip.Id, cancellationToken) is { Status: "PENDING" } request)
        {
            var decide = await decideHandler.HandleAsync(new DecideApprovalRequestCommand(request.Id, command.CancelledBy, "Reject", $"Cancelled: {command.Reason}"), cancellationToken);
            if (!decide.IsSuccess)
                return Result.Failure<TripResponse>(decide.Error!);
        }

        trip.Status = TripStatus.Cancelled;
        trip.CancelledBy = command.CancelledBy;
        trip.CancelledAt = DateTimeOffset.UtcNow;
        trip.CancelReason = command.Reason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        await createNotificationHandler.HandleAsync(new CreateNotificationCommand("TRIP", trip.Id.ToString(), trip.BranchId, "CANCELLED",
            "ACTIVITY", TripShared.NotificationMessage($"cancelled this trip: {trip.CancelReason}"), command.CancelledBy), cancellationToken);
        return Result.Success(await TripShared.LoadResponseAsync(dbContext, trip.Id, cancellationToken));
    }
}
