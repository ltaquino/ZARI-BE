using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanPayments.ApproveCancellation;
using ZARI.Application.Features.Loan.LoanPayments.Create;
using ZARI.Application.Features.Loan.LoanPayments.Get;
using ZARI.Application.Features.Loan.LoanPayments.GetAll;
using ZARI.Application.Features.Loan.LoanPayments.RejectCancellation;
using ZARI.Application.Features.Loan.LoanPayments.RequestCancellation;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class LoanPaymentEndpoints
{
    public static void MapLoanPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/loan-payments")
            .WithTags("LoanPayments")
            .WithGroupName("Loan")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllLoanPayments")
            .WithSummary("Get all loan payments");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetLoanPaymentById")
            .WithSummary("Get a loan payment by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateLoanPaymentCommand>>()
            .WithName("CreateLoanPayment")
            .WithSummary("Create and immediately post a loan payment — allocates oldest-installment-first, posts the GL journal and ledger entry");

        group.MapPost("/{id:guid}/request-cancellation", RequestCancellation)
            .WithName("RequestLoanPaymentCancellation")
            .WithSummary("Request cancellation of a posted loan payment");

        group.MapPost("/{id:guid}/approve-cancellation", ApproveCancellation)
            .WithName("ApproveLoanPaymentCancellation")
            .WithSummary("Approve a cancellation request — reverses the GL journal and unwinds the schedule-line allocations");

        group.MapPost("/{id:guid}/reject-cancellation", RejectCancellation)
            .WithName("RejectLoanPaymentCancellation")
            .WithSummary("Reject a cancellation request — the document stands as posted");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllLoanPaymentsQuery, Result<List<LoanPaymentResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllLoanPaymentsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetLoanPaymentQuery, Result<LoanPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetLoanPaymentQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateLoanPaymentCommand command,
        ICommandHandler<CreateLoanPaymentCommand, Result<LoanPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetLoanPaymentById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> RequestCancellation(
        Guid id,
        RequestLoanPaymentCancellationRequest request,
        IValidator<RequestLoanPaymentCancellationCommand> validator,
        ICommandHandler<RequestLoanPaymentCancellationCommand, Result<LoanPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RequestLoanPaymentCancellationCommand(id, request.RequestedBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> ApproveCancellation(
        Guid id,
        DecideLoanPaymentRequest request,
        IValidator<ApproveLoanPaymentCancellationCommand> validator,
        ICommandHandler<ApproveLoanPaymentCancellationCommand, Result<LoanPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveLoanPaymentCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RejectCancellation(
        Guid id,
        DecideLoanPaymentRequiredCommentRequest request,
        IValidator<RejectLoanPaymentCancellationCommand> validator,
        ICommandHandler<RejectLoanPaymentCancellationCommand, Result<LoanPaymentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectLoanPaymentCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record RequestLoanPaymentCancellationRequest(string RequestedBy, string Reason);
public sealed record DecideLoanPaymentRequest(string ApproverUserId, string? Comments);
public sealed record DecideLoanPaymentRequiredCommentRequest(string ApproverUserId, string Comments);
