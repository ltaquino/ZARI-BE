using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.OutgoingPayments.ApproveCancellation;
using ZARI.Application.Features.Purchasing.OutgoingPayments.Approve;
using ZARI.Application.Features.Purchasing.OutgoingPayments.Cancel;
using ZARI.Application.Features.Purchasing.OutgoingPayments.Create;
using ZARI.Application.Features.Purchasing.OutgoingPayments.Delete;
using ZARI.Application.Features.Purchasing.OutgoingPayments.Get;
using ZARI.Application.Features.Purchasing.OutgoingPayments.GetAll;
using ZARI.Application.Features.Purchasing.OutgoingPayments.Reject;
using ZARI.Application.Features.Purchasing.OutgoingPayments.RejectCancellation;
using ZARI.Application.Features.Purchasing.OutgoingPayments.RequestCancellation;
using ZARI.Application.Features.Purchasing.OutgoingPayments.Submit;
using ZARI.Application.Features.Purchasing.OutgoingPayments.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class OutgoingPaymentEndpoints
{
    public static void MapOutgoingPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/outgoing-payments")
            .WithTags("OutgoingPayments")
            .WithGroupName("Purchasing")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllOutgoingPayments")
            .WithSummary("Get all outgoing payments");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetOutgoingPaymentById")
            .WithSummary("Get an outgoing payment by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateOutgoingPaymentCommand>>()
            .WithName("CreateOutgoingPayment")
            .WithSummary("Create a draft outgoing payment against one or more posted AP invoices");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateOutgoingPayment")
            .WithSummary("Update a draft outgoing payment");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteOutgoingPayment")
            .WithSummary("Delete a draft outgoing payment");

        group.MapPost("/{id:guid}/submit", Submit)
            .WithName("SubmitOutgoingPayment")
            .WithSummary("Submit a draft outgoing payment for approval");

        group.MapPost("/{id:guid}/approve", Approve)
            .WithName("ApproveOutgoingPayment")
            .WithSummary("Approve a pending outgoing payment — posts the AP-to-cash/bank GL journal and marks its invoices paid");

        group.MapPost("/{id:guid}/reject", Reject)
            .WithName("RejectOutgoingPayment")
            .WithSummary("Reject a pending outgoing payment back to draft");

        group.MapPost("/{id:guid}/cancel", Cancel)
            .WithName("CancelOutgoingPayment")
            .WithSummary("Cancel a draft or pending-approval outgoing payment directly");

        group.MapPost("/{id:guid}/request-cancellation", RequestCancellation)
            .WithName("RequestOutgoingPaymentCancellation")
            .WithSummary("Request cancellation of a posted outgoing payment");

        group.MapPost("/{id:guid}/approve-cancellation", ApproveCancellation)
            .WithName("ApproveOutgoingPaymentCancellation")
            .WithSummary("Approve a cancellation request — reverses the GL journal and unpays its invoices");

        group.MapPost("/{id:guid}/reject-cancellation", RejectCancellation)
            .WithName("RejectOutgoingPaymentCancellation")
            .WithSummary("Reject a cancellation request — the document stands as posted");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllOutgoingPaymentsQuery, Result<List<OutgoingPaymentResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllOutgoingPaymentsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetOutgoingPaymentQuery, Result<OutgoingPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetOutgoingPaymentQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateOutgoingPaymentCommand command,
        ICommandHandler<CreateOutgoingPaymentCommand, Result<OutgoingPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetOutgoingPaymentById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateOutgoingPaymentRequest request,
        IValidator<UpdateOutgoingPaymentCommand> validator,
        ICommandHandler<UpdateOutgoingPaymentCommand, Result<OutgoingPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateOutgoingPaymentCommand(
            id, request.BankAccountId, request.PaymentDate, request.RefNo, request.Remarks, request.CostCenterId, request.UpdatedBy, request.Lines);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteOutgoingPaymentCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteOutgoingPaymentCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(
        Guid id,
        SubmitOutgoingPaymentRequest request,
        IValidator<SubmitOutgoingPaymentCommand> validator,
        ICommandHandler<SubmitOutgoingPaymentCommand, Result<OutgoingPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new SubmitOutgoingPaymentCommand(id, request.RequestedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Approve(
        Guid id,
        DecideOutgoingPaymentRequest request,
        IValidator<ApproveOutgoingPaymentCommand> validator,
        ICommandHandler<ApproveOutgoingPaymentCommand, Result<OutgoingPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveOutgoingPaymentCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reject(
        Guid id,
        DecideOutgoingPaymentRequiredCommentRequest request,
        IValidator<RejectOutgoingPaymentCommand> validator,
        ICommandHandler<RejectOutgoingPaymentCommand, Result<OutgoingPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectOutgoingPaymentCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Cancel(
        Guid id,
        CancelOutgoingPaymentRequest request,
        IValidator<CancelOutgoingPaymentCommand> validator,
        ICommandHandler<CancelOutgoingPaymentCommand, Result<OutgoingPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelOutgoingPaymentCommand(id, request.CancelledBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RequestCancellation(
        Guid id,
        RequestOutgoingPaymentCancellationRequest request,
        IValidator<RequestOutgoingPaymentCancellationCommand> validator,
        ICommandHandler<RequestOutgoingPaymentCancellationCommand, Result<OutgoingPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RequestOutgoingPaymentCancellationCommand(id, request.RequestedBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> ApproveCancellation(
        Guid id,
        DecideOutgoingPaymentRequest request,
        IValidator<ApproveOutgoingPaymentCancellationCommand> validator,
        ICommandHandler<ApproveOutgoingPaymentCancellationCommand, Result<OutgoingPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveOutgoingPaymentCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RejectCancellation(
        Guid id,
        DecideOutgoingPaymentRequiredCommentRequest request,
        IValidator<RejectOutgoingPaymentCancellationCommand> validator,
        ICommandHandler<RejectOutgoingPaymentCancellationCommand, Result<OutgoingPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectOutgoingPaymentCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateOutgoingPaymentRequest(
    Guid BankAccountId,
    DateTimeOffset PaymentDate,
    string? RefNo,
    string? Remarks,
    Guid? CostCenterId,
    string? UpdatedBy,
    List<OutgoingPaymentLineInput> Lines);

public sealed record SubmitOutgoingPaymentRequest(string RequestedBy);
public sealed record DecideOutgoingPaymentRequest(string ApproverUserId, string? Comments);
public sealed record DecideOutgoingPaymentRequiredCommentRequest(string ApproverUserId, string Comments);
public sealed record CancelOutgoingPaymentRequest(string CancelledBy, string Reason);
public sealed record RequestOutgoingPaymentCancellationRequest(string RequestedBy, string Reason);
