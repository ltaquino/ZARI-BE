using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.CustomerPayments.ApproveCancellation;
using ZARI.Application.Features.Sales.CustomerPayments.Approve;
using ZARI.Application.Features.Sales.CustomerPayments.Cancel;
using ZARI.Application.Features.Sales.CustomerPayments.Create;
using ZARI.Application.Features.Sales.CustomerPayments.Delete;
using ZARI.Application.Features.Sales.CustomerPayments.Get;
using ZARI.Application.Features.Sales.CustomerPayments.GetAll;
using ZARI.Application.Features.Sales.CustomerPayments.Reject;
using ZARI.Application.Features.Sales.CustomerPayments.RejectCancellation;
using ZARI.Application.Features.Sales.CustomerPayments.RequestCancellation;
using ZARI.Application.Features.Sales.CustomerPayments.Submit;
using ZARI.Application.Features.Sales.CustomerPayments.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class CustomerPaymentEndpoints
{
    public static void MapCustomerPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customer-payments")
            .WithTags("CustomerPayments")
            .WithGroupName("Sales")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllCustomerPayments")
            .WithSummary("Get all customer payments");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetCustomerPaymentById")
            .WithSummary("Get a customer payment by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateCustomerPaymentCommand>>()
            .WithName("CreateCustomerPayment")
            .WithSummary("Create a draft customer payment (or post it directly — converting AR into cash and updating invoice statuses — if quick-post is enabled)");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateCustomerPayment")
            .WithSummary("Update a draft customer payment");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteCustomerPayment")
            .WithSummary("Delete a draft customer payment");

        group.MapPost("/{id:guid}/submit", Submit)
            .WithName("SubmitCustomerPayment")
            .WithSummary("Submit a draft customer payment for approval");

        group.MapPost("/{id:guid}/approve", Approve)
            .WithName("ApproveCustomerPayment")
            .WithSummary("Approve a pending customer payment — posts Dr Cash/Bank, Cr Accounts Receivable and updates invoice statuses");

        group.MapPost("/{id:guid}/reject", Reject)
            .WithName("RejectCustomerPayment")
            .WithSummary("Reject a pending customer payment back to draft");

        group.MapPost("/{id:guid}/cancel", Cancel)
            .WithName("CancelCustomerPayment")
            .WithSummary("Cancel a draft or pending-approval customer payment directly");

        group.MapPost("/{id:guid}/request-cancellation", RequestCancellation)
            .WithName("RequestCustomerPaymentCancellation")
            .WithSummary("Request cancellation of a posted customer payment");

        group.MapPost("/{id:guid}/approve-cancellation", ApproveCancellation)
            .WithName("ApproveCustomerPaymentCancellation")
            .WithSummary("Approve a cancellation request — reverses the posted GL journal and re-derives invoice statuses");

        group.MapPost("/{id:guid}/reject-cancellation", RejectCancellation)
            .WithName("RejectCustomerPaymentCancellation")
            .WithSummary("Reject a cancellation request — the document stands as posted");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllCustomerPaymentsQuery, Result<List<CustomerPaymentResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllCustomerPaymentsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetCustomerPaymentQuery, Result<CustomerPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCustomerPaymentQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateCustomerPaymentCommand command,
        ICommandHandler<CreateCustomerPaymentCommand, Result<CustomerPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetCustomerPaymentById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateCustomerPaymentRequest request,
        IValidator<UpdateCustomerPaymentCommand> validator,
        ICommandHandler<UpdateCustomerPaymentCommand, Result<CustomerPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCustomerPaymentCommand(
            id, request.PaymentMethod, request.CashAccountId, request.PaymentDate, request.ReferenceNo,
            request.Remarks, request.CostCenterId, request.UpdatedBy, request.Lines);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteCustomerPaymentCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteCustomerPaymentCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(
        Guid id,
        SubmitCustomerPaymentRequest request,
        IValidator<SubmitCustomerPaymentCommand> validator,
        ICommandHandler<SubmitCustomerPaymentCommand, Result<CustomerPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new SubmitCustomerPaymentCommand(id, request.RequestedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Approve(
        Guid id,
        DecideCustomerPaymentRequest request,
        IValidator<ApproveCustomerPaymentCommand> validator,
        ICommandHandler<ApproveCustomerPaymentCommand, Result<CustomerPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveCustomerPaymentCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reject(
        Guid id,
        DecideCustomerPaymentRequiredCommentRequest request,
        IValidator<RejectCustomerPaymentCommand> validator,
        ICommandHandler<RejectCustomerPaymentCommand, Result<CustomerPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectCustomerPaymentCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Cancel(
        Guid id,
        CancelCustomerPaymentRequest request,
        IValidator<CancelCustomerPaymentCommand> validator,
        ICommandHandler<CancelCustomerPaymentCommand, Result<CustomerPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelCustomerPaymentCommand(id, request.CancelledBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RequestCancellation(
        Guid id,
        RequestCustomerPaymentCancellationRequest request,
        IValidator<RequestCustomerPaymentCancellationCommand> validator,
        ICommandHandler<RequestCustomerPaymentCancellationCommand, Result<CustomerPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RequestCustomerPaymentCancellationCommand(id, request.RequestedBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> ApproveCancellation(
        Guid id,
        DecideCustomerPaymentRequest request,
        IValidator<ApproveCustomerPaymentCancellationCommand> validator,
        ICommandHandler<ApproveCustomerPaymentCancellationCommand, Result<CustomerPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveCustomerPaymentCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RejectCancellation(
        Guid id,
        DecideCustomerPaymentRequiredCommentRequest request,
        IValidator<RejectCustomerPaymentCancellationCommand> validator,
        ICommandHandler<RejectCustomerPaymentCancellationCommand, Result<CustomerPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectCustomerPaymentCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateCustomerPaymentRequest(
    string PaymentMethod,
    Guid CashAccountId,
    DateTimeOffset PaymentDate,
    string? ReferenceNo,
    string? Remarks,
    Guid? CostCenterId,
    string? UpdatedBy,
    List<CustomerPaymentLineInput> Lines);

// CustomerPaymentLineInput is defined in CreateCustomerPaymentCommand.cs and reused as-is for
// Update — same convention as OutgoingPaymentLineInput/SalesInvoiceLineInput.

public sealed record SubmitCustomerPaymentRequest(string RequestedBy);
public sealed record DecideCustomerPaymentRequest(string ApproverUserId, string? Comments);
public sealed record DecideCustomerPaymentRequiredCommentRequest(string ApproverUserId, string Comments);
public sealed record CancelCustomerPaymentRequest(string CancelledBy, string Reason);
public sealed record RequestCustomerPaymentCancellationRequest(string RequestedBy, string Reason);
