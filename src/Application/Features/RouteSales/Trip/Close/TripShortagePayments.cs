namespace ZARI.Application.Features.RouteSales.Trips.Close;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.RouteSales.Trips.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed record TripShortagePaymentResponse(
    Guid Id, Guid TripId, decimal Amount, Guid PaymentMethodId, string PaymentMethodName, DateTimeOffset PaidAt,
    string? ReferenceNo, string? Remarks, string? ReceivedBy, DateTimeOffset CreatedAt);

/// <summary>
/// The driver pays in (part of) a DRIVER_PAYS shortage: Dr the payment method's account / Cr 1220 Due from
/// Employees, which the close debited. Several payments are fine; together they can't exceed the balance.
/// </summary>
public sealed record RecordTripShortagePaymentCommand(Guid TripId, decimal Amount, Guid PaymentMethodId, DateTimeOffset PaidAt,
    string? ReferenceNo, string? Remarks, string? ReceivedBy) : ICommand<Result<TripShortagePaymentResponse>>;

public sealed class RecordTripShortagePaymentValidator : AbstractValidator<RecordTripShortagePaymentCommand>
{
    public RecordTripShortagePaymentValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaymentMethodId).NotEmpty();
        RuleFor(x => x.PaidAt).NotEmpty();
        RuleFor(x => x.ReferenceNo).MaximumLength(150);
        RuleFor(x => x.Remarks).MaximumLength(300);
    }
}

public sealed class RecordTripShortagePaymentCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    IPermissionService permissionService) : ICommandHandler<RecordTripShortagePaymentCommand, Result<TripShortagePaymentResponse>>
{
    public const string JournalTable = "TripShortagePayment";

    public async Task<Result<TripShortagePaymentResponse>> HandleAsync(RecordTripShortagePaymentCommand command, CancellationToken cancellationToken = default)
    {
        var ct = cancellationToken;
        var trip = await dbContext.Trips.FirstOrDefaultAsync(t => t.Id == command.TripId, ct);
        if (trip is null)
            return Fail(Error.NotFound("Trip.NotFound", $"Trip with ID '{command.TripId}' was not found."));
        if (!await permissionService.HasPermissionOnBranchAsync("TRIPS", FormAction.Approve, trip.BranchId, ct))
            return Fail(Error.Forbidden("Trip.Forbidden", "You do not have permission to settle trip shortages for this branch."));
        if (trip.Status != TripStatus.Closed || trip.ShortageSettlement != TripShortageSettlement.DriverPays)
            return Fail(Error.Validation("Trip.NoDriverShortage", "Only a closed trip whose shortage the driver pays can take a shortage payment."));
        var balance = TripShared.ShortageBalance(trip);
        if (command.Amount > balance)
            return Fail(Error.Validation("Trip.OverpaidShortage", $"The driver owes {balance:N2} on this trip."));

        var method = await dbContext.PaymentMethods.AsNoTracking().FirstOrDefaultAsync(m => m.Id == command.PaymentMethodId, ct);
        if (method is null)
            return Fail(Error.NotFound("PaymentMethod.NotFound", "The payment method was not found."));
        var dueFrom = await TripShared.AccountIdAsync(dbContext, "1220", "Due from Employees", ct);
        if (!dueFrom.IsSuccess)
            return Fail(dueFrom.Error!);

        var payment = new TripShortagePayment
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            Amount = Math.Round(command.Amount, 4),
            PaymentMethodId = method.Id,
            PaidAt = command.PaidAt,
            ReferenceNo = command.ReferenceNo?.Trim(),
            Remarks = command.Remarks?.Trim(),
            CreatedBy = command.ReceivedBy,
        };
        var post = await postGlJournalHandler.HandleAsync(new PostGlJournalCommand(trip.BranchId, command.PaidAt, "ROUTE_SALES", JournalTable,
            payment.Id.ToString(), $"Trip {trip.TripNo} — driver's shortage payment",
            [
                new PostGlJournalLineInput(method.GlAccountId, null, payment.Amount, 0, null),
                new PostGlJournalLineInput(dueFrom.Value, null, 0, payment.Amount, null),
            ]), ct);
        if (!post.IsSuccess)
            return Fail(post.Error!);

        // The journal posts in its own save; reload the trip so its balance is current, then record both.
        dbContext.ChangeTracker.Clear();
        var paid = await dbContext.Trips.FirstAsync(t => t.Id == trip.Id, ct);
        paid.ShortagePaid += payment.Amount;
        dbContext.TripShortagePayments.Add(payment);
        await dbContext.SaveChangesAsync(ct);

        return Result.Success(new TripShortagePaymentResponse(payment.Id, trip.Id, payment.Amount, method.Id, method.Name, payment.PaidAt,
            payment.ReferenceNo, payment.Remarks, payment.CreatedBy, payment.CreatedAt));
    }

    private static Result<TripShortagePaymentResponse> Fail(Error error) => Result.Failure<TripShortagePaymentResponse>(error);
}

public sealed record GetTripShortagePaymentsQuery(Guid TripId) : IQuery<Result<List<TripShortagePaymentResponse>>>;

public sealed class GetTripShortagePaymentsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetTripShortagePaymentsQuery, Result<List<TripShortagePaymentResponse>>>
{
    public async Task<Result<List<TripShortagePaymentResponse>>> HandleAsync(GetTripShortagePaymentsQuery query, CancellationToken cancellationToken = default)
    {
        var branchId = await dbContext.Trips.Where(t => t.Id == query.TripId).Select(t => t.BranchId).FirstOrDefaultAsync(cancellationToken);
        if (branchId is null)
            return Result.Failure<List<TripShortagePaymentResponse>>(Error.NotFound("Trip.NotFound", $"Trip with ID '{query.TripId}' was not found."));
        if (!await permissionService.HasPermissionOnBranchAsync("TRIPS", FormAction.View, branchId, cancellationToken))
            return Result.Failure<List<TripShortagePaymentResponse>>(Error.Forbidden("Trip.Forbidden", "You do not have permission to view trips for this branch."));

        var payments = await dbContext.TripShortagePayments.AsNoTracking().Include(p => p.PaymentMethod)
            .Where(p => p.TripId == query.TripId).OrderBy(p => p.PaidAt).ToListAsync(cancellationToken);
        return Result.Success(payments.Select(p => new TripShortagePaymentResponse(p.Id, p.TripId, p.Amount, p.PaymentMethodId, p.PaymentMethod.Name,
            p.PaidAt, p.ReferenceNo, p.Remarks, p.CreatedBy, p.CreatedAt)).ToList());
    }
}
