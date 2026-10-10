using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.Trips.Close;
using ZARI.Application.Features.RouteSales.Trips.Create;
using ZARI.Application.Features.RouteSales.Trips.Get;
using ZARI.Application.Features.RouteSales.Trips.Handheld;
using ZARI.Application.Features.RouteSales.Trips.LoadWorkflow;
using ZARI.Application.Features.RouteSales.Trips.RouteSales;
using ZARI.Application.Features.RouteSales.Trips.Shared;
using ZARI.Application.Features.RouteSales.Trips.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

/// <summary>
/// Route Sales trips (ZARI-FE/frs/route-sales/RouteSalesContext.md §5). Back office: plan, approve
/// the load-out, close. Handheld (the trip's driver): /mine, accept-load, route-sales, voids,
/// returns, collections, proof of delivery, end. Every handheld write is idempotent, so the
/// device's outbox can retry anything that may or may not have reached the server.
/// </summary>
public static class TripEndpoints
{
    private const long MaxPodFileBytes = 5 * 1024 * 1024;
    private static readonly string[] PodExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public static void MapTripEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/trips").WithTags("Trips").WithGroupName("RouteSales").RequireAuthorization();

        group.MapGet("/", GetAll).WithName("GetAllTrips").WithSummary("Trips, optionally by branch / status / trip date");
        group.MapGet("/available-sales-orders", GetAvailableSalesOrders).WithName("GetTripAvailableSalesOrders")
            .WithSummary("Approved sales orders a trip can carry (branch, still undelivered, not on another open trip)");
        group.MapGet("/drivers", GetDrivers).WithName("GetTripDrivers")
            .WithSummary("Active users of a branch who can drive its trips, with their HR employee link");
        group.MapGet("/mine", GetMine).WithName("GetMyTrips").WithSummary("The signed-in driver's trips on the road, with everything the handheld needs offline");
        group.MapGet("/{id:guid}", GetById).WithName("GetTripById").WithSummary("A trip with its per-item quantities");
        group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<CreateTripCommand>>().WithName("CreateTrip").WithSummary("Plan a trip (DRAFT)");
        group.MapPut("/{id:guid}", Update).WithName("UpdateTrip").WithSummary("Re-plan a DRAFT trip");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteTrip").WithSummary("Delete a DRAFT trip");

        group.MapPost("/{id:guid}/submit", Submit).WithName("SubmitTrip").WithSummary("Submit the load-out for approval");
        group.MapPost("/{id:guid}/approve-load", ApproveLoad).WithName("ApproveTripLoad").WithSummary("Approve the load-out — stock moves onto the truck");
        group.MapPost("/{id:guid}/reject-load", RejectLoad).WithName("RejectTripLoad").WithSummary("Send the trip back to DRAFT");
        group.MapPost("/{id:guid}/cancel", Cancel).WithName("CancelTrip").WithSummary("Cancel a trip that hasn't been loaded");

        group.MapPost("/{id:guid}/accept-load", AcceptLoad).WithName("AcceptTripLoad").WithSummary("Driver: count and accept the load");
        group.MapPost("/{id:guid}/route-sales", RecordSale).AddEndpointFilter<ValidationFilter<RecordRouteSaleCommand>>()
            .WithName("RecordRouteSale").WithSummary("Driver: record an invoice issued at a stop (idempotent on the invoice number)");
        group.MapPost("/{id:guid}/route-sales/void", VoidInvoice).WithName("VoidRouteInvoice").WithSummary("Void an invoice number issued on the road");
        group.MapPost("/{id:guid}/returns", RecordReturn).AddEndpointFilter<ValidationFilter<RecordTripReturnCommand>>()
            .WithName("RecordTripReturn").WithSummary("Driver: customer return onto the truck (idempotent on clientRef)");
        group.MapPost("/{id:guid}/collections", RecordCollection).AddEndpointFilter<ValidationFilter<RecordTripCollectionCommand>>()
            .WithName("RecordTripCollection").WithSummary("Driver: collect against open invoices (idempotent on clientRef)");
        group.MapPost("/{id:guid}/refusals", RecordRefusal).AddEndpointFilter<ValidationFilter<RecordTripRefusalCommand>>()
            .WithName("RecordTripRefusal").WithSummary("Driver: ordered goods refused at a stop, with reasons (no invoice; idempotent on clientRef)");
        group.MapPost("/{id:guid}/end", End).WithName("EndTrip").WithSummary("Driver: end the trip with the remaining count and cash");

        group.MapPost("/{id:guid}/approve-close", ApproveClose).WithName("ApproveTripClose").WithSummary("Close the trip: unload, settle variances, settle shortages (payroll / driver pays / write-off)");
        group.MapPost("/{id:guid}/shortage-payments", RecordShortagePayment).WithName("RecordTripShortagePayment")
            .WithSummary("The driver pays in (part of) a DRIVER_PAYS shortage");
        group.MapGet("/{id:guid}/shortage-payments", GetShortagePayments).WithName("GetTripShortagePayments").WithSummary("The driver's shortage payments on a trip");
        group.MapPost("/{id:guid}/reject-close", RejectClose).WithName("RejectTripClose").WithSummary("Send an ended trip back to the driver");

        app.MapPost("/api/route-sales/{id:guid}/pod", UploadPod)
            .WithTags("Trips").WithGroupName("RouteSales").RequireAuthorization().DisableAntiforgery()
            .WithName("UploadRouteSalePod").WithSummary("Driver: proof of delivery (receiver name, signature and/or photo)");
    }

    private static IResult ToHttp<T>(Result<T> result) => result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();

    private static async Task<IResult> GetAll(string? branchId, string? status, DateOnly? tripDate,
        IQueryHandler<GetAllTripsQuery, Result<List<TripResponse>>> handler, CancellationToken ct) =>
        ToHttp(await handler.HandleAsync(new GetAllTripsQuery(branchId, status, tripDate), ct));

    private static async Task<IResult> GetAvailableSalesOrders(string branchId, Guid? excludeTripId,
        IQueryHandler<GetAvailableSalesOrdersQuery, Result<List<AvailableSalesOrder>>> handler, CancellationToken ct) =>
        ToHttp(await handler.HandleAsync(new GetAvailableSalesOrdersQuery(branchId, excludeTripId), ct));

    private static async Task<IResult> GetDrivers(string branchId, IQueryHandler<GetTripDriversQuery, Result<List<TripDriverOption>>> handler, CancellationToken ct) =>
        ToHttp(await handler.HandleAsync(new GetTripDriversQuery(branchId), ct));

    private static async Task<IResult> GetMine(IQueryHandler<GetMyTripsQuery, Result<List<DeviceTripBundle>>> handler, CancellationToken ct) =>
        ToHttp(await handler.HandleAsync(new GetMyTripsQuery(), ct));

    private static async Task<IResult> GetById(Guid id, IQueryHandler<GetTripQuery, Result<TripResponse>> handler, CancellationToken ct) =>
        ToHttp(await handler.HandleAsync(new GetTripQuery(id), ct));

    private static async Task<IResult> Create(CreateTripCommand command, ICommandHandler<CreateTripCommand, Result<TripResponse>> handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(command, ct);
        return result.IsSuccess ? TypedResults.CreatedAtRoute(result.Value, "GetTripById", new { id = result.Value!.Id }) : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(Guid id, UpdateTripRequest r, IValidator<UpdateTripCommand> validator,
        ICommandHandler<UpdateTripCommand, Result<TripResponse>> handler, CancellationToken ct)
    {
        var command = new UpdateTripCommand(id, r.VehicleId, r.RouteId, r.SourceWarehouseId, r.DriverUserId, r.DriverEmployeeId, r.HelperNames,
            r.TripDate, r.Remarks, r.SalesOrderIds, r.ExtraLines);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;
        return ToHttp(await handler.HandleAsync(command, ct));
    }

    private static async Task<IResult> Delete(Guid id, ICommandHandler<DeleteTripCommand, Result> handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(new DeleteTripCommand(id), ct);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(Guid id, SubmitTripRequest r, ICommandHandler<SubmitTripCommand, Result<TripResponse>> handler, CancellationToken ct) =>
        ToHttp(await handler.HandleAsync(new SubmitTripCommand(id, r.RequestedBy), ct));

    private static async Task<IResult> ApproveLoad(Guid id, TripDecisionRequest r, ICommandHandler<ApproveTripLoadCommand, Result<TripResponse>> handler, CancellationToken ct) =>
        ToHttp(await handler.HandleAsync(new ApproveTripLoadCommand(id, r.ApproverUserId, r.Comments), ct));

    private static async Task<IResult> RejectLoad(Guid id, TripDecisionRequest r, ICommandHandler<RejectTripLoadCommand, Result<TripResponse>> handler, CancellationToken ct) =>
        ToHttp(await handler.HandleAsync(new RejectTripLoadCommand(id, r.ApproverUserId, r.Comments ?? ""), ct));

    private static async Task<IResult> Cancel(Guid id, CancelTripRequest r, ICommandHandler<CancelTripCommand, Result<TripResponse>> handler, CancellationToken ct) =>
        ToHttp(await handler.HandleAsync(new CancelTripCommand(id, r.CancelledBy, r.Reason), ct));

    private static async Task<IResult> AcceptLoad(Guid id, TripCountsRequest r, ICommandHandler<AcceptTripLoadCommand, Result<AcceptTripLoadResponse>> handler, CancellationToken ct) =>
        ToHttp(await handler.HandleAsync(new AcceptTripLoadCommand(id, r.Counts), ct));

    private static async Task<IResult> RecordSale(Guid id, RecordRouteSaleCommand command,
        ICommandHandler<RecordRouteSaleCommand, Result<RouteSaleResponse>> handler, CancellationToken ct) =>
        ToHttp(await handler.HandleAsync(command with { TripId = id }, ct));

    private static async Task<IResult> VoidInvoice(Guid id, VoidRouteInvoiceRequest r, ICommandHandler<VoidRouteInvoiceCommand, Result<RouteSaleResponse>> handler, CancellationToken ct) =>
        ToHttp(await handler.HandleAsync(new VoidRouteInvoiceCommand(id, r.BirOrSeriesNumber, r.Reason), ct));

    private static async Task<IResult> RecordReturn(Guid id, RecordTripReturnCommand command, ICommandHandler<RecordTripReturnCommand, Result<Guid>> handler, CancellationToken ct) =>
        ToHttp(await handler.HandleAsync(command with { TripId = id }, ct));

    private static async Task<IResult> RecordCollection(Guid id, RecordTripCollectionCommand command, ICommandHandler<RecordTripCollectionCommand, Result<Guid>> handler, CancellationToken ct) =>
        ToHttp(await handler.HandleAsync(command with { TripId = id }, ct));

    private static async Task<IResult> End(Guid id, EndTripRequest r, ICommandHandler<EndTripCommand, Result<TripResponse>> handler, CancellationToken ct) =>
        ToHttp(await handler.HandleAsync(new EndTripCommand(id, r.CashRemitted, r.Counts), ct));

    private static async Task<IResult> RecordRefusal(Guid id, RecordTripRefusalCommand command, ICommandHandler<RecordTripRefusalCommand, Result<Guid>> handler, CancellationToken ct) =>
        ToHttp(await handler.HandleAsync(command with { TripId = id }, ct));

    private static async Task<IResult> RecordShortagePayment(Guid id, TripShortagePaymentRequest r,
        ICommandHandler<RecordTripShortagePaymentCommand, Result<TripShortagePaymentResponse>> handler, CancellationToken ct) =>
        ToHttp(await handler.HandleAsync(new RecordTripShortagePaymentCommand(id, r.Amount, r.PaymentMethodId, r.PaidAt, r.ReferenceNo, r.Remarks, r.ReceivedBy), ct));

    private static async Task<IResult> GetShortagePayments(Guid id, IQueryHandler<GetTripShortagePaymentsQuery, Result<List<TripShortagePaymentResponse>>> handler, CancellationToken ct) =>
        ToHttp(await handler.HandleAsync(new GetTripShortagePaymentsQuery(id), ct));

    private static async Task<IResult> ApproveClose(Guid id, ApproveTripCloseRequest r, ICommandHandler<ApproveTripCloseCommand, Result<TripResponse>> handler, CancellationToken ct) =>
        ToHttp(await handler.HandleAsync(new ApproveTripCloseCommand(id, r.ApproverUserId, r.Comments, r.ShortageSettlement), ct));

    private static async Task<IResult> RejectClose(Guid id, TripDecisionRequest r, ICommandHandler<RejectTripCloseCommand, Result<TripResponse>> handler, CancellationToken ct) =>
        ToHttp(await handler.HandleAsync(new RejectTripCloseCommand(id, r.ApproverUserId, r.Comments ?? ""), ct));

    /// <summary>Same plain file-system storage as POS promo slides: wwwroot/uploads/route-sales-pod.</summary>
    private static async Task<IResult> UploadPod(Guid id, IFormFile? signature, IFormFile? photo, [Microsoft.AspNetCore.Mvc.FromForm] string? receivedByName,
        IWebHostEnvironment env, ICommandHandler<SetRouteSalePodCommand, Result> handler, CancellationToken ct)
    {
        if (signature is null && photo is null && string.IsNullOrWhiteSpace(receivedByName))
            return Results.Problem(title: "Validation Error", detail: "Send a signature, a photo or the receiver's name.", statusCode: StatusCodes.Status400BadRequest);

        async Task<(string? Url, string? Error)> Save(IFormFile? file)
        {
            if (file is null) return (null, null);
            if (file.Length == 0 || file.Length > MaxPodFileBytes) return (null, "Each image must be between 1 byte and 5MB.");
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!PodExtensions.Contains(ext)) return (null, $"Only {string.Join(", ", PodExtensions)} files are allowed.");
            var dir = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads", "route-sales-pod");
            Directory.CreateDirectory(dir);
            var name = $"{id:N}-{Guid.NewGuid():N}{ext}";
            await using (var stream = File.Create(Path.Combine(dir, name)))
                await file.CopyToAsync(stream, ct);
            return ($"/uploads/route-sales-pod/{name}", null);
        }

        var sig = await Save(signature);
        var pic = await Save(photo);
        if ((sig.Error ?? pic.Error) is { } error)
            return Results.Problem(title: "Validation Error", detail: error, statusCode: StatusCodes.Status400BadRequest);

        var result = await handler.HandleAsync(new SetRouteSalePodCommand(id, receivedByName, sig.Url, pic.Url), ct);
        return result.IsSuccess ? TypedResults.Ok(new { signatureUrl = sig.Url, photoUrl = pic.Url }) : result.ToProblemDetails();
    }
}

public sealed record UpdateTripRequest(Guid VehicleId, Guid? RouteId, Guid SourceWarehouseId, string DriverUserId, Guid? DriverEmployeeId,
    string? HelperNames, DateTimeOffset TripDate, string? Remarks, List<Guid> SalesOrderIds, List<TripExtraLineInput> ExtraLines);
public sealed record SubmitTripRequest(string RequestedBy);
public sealed record TripDecisionRequest(string ApproverUserId, string? Comments);
public sealed record CancelTripRequest(string CancelledBy, string Reason);
public sealed record TripCountsRequest(List<TripCountInput> Counts);
public sealed record VoidRouteInvoiceRequest(string BirOrSeriesNumber, string Reason);
public sealed record EndTripRequest(decimal CashRemitted, List<TripCountInput> Counts);
/// <summary>ShortageSettlement: PAYROLL, DRIVER_PAYS or WRITE_OFF; omitted = PAYROLL when the driver is linked to HR, else DRIVER_PAYS.</summary>
public sealed record ApproveTripCloseRequest(string ApproverUserId, string? Comments, string? ShortageSettlement = null);
public sealed record TripShortagePaymentRequest(decimal Amount, Guid PaymentMethodId, DateTimeOffset PaidAt, string? ReferenceNo, string? Remarks, string? ReceivedBy);
