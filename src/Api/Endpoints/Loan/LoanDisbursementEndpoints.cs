using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanDisbursements.Approve;
using ZARI.Application.Features.Loan.LoanDisbursements.ApproveCancellation;
using ZARI.Application.Features.Loan.LoanDisbursements.Cancel;
using ZARI.Application.Features.Loan.LoanDisbursements.Create;
using ZARI.Application.Features.Loan.LoanDisbursements.Delete;
using ZARI.Application.Features.Loan.LoanDisbursements.Get;
using ZARI.Application.Features.Loan.LoanDisbursements.GetAll;
using ZARI.Application.Features.Loan.LoanDisbursements.Reject;
using ZARI.Application.Features.Loan.LoanDisbursements.RejectCancellation;
using ZARI.Application.Features.Loan.LoanDisbursements.RequestCancellation;
using ZARI.Application.Features.Loan.LoanDisbursements.Submit;
using ZARI.Application.Features.Loan.LoanDisbursements.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class LoanDisbursementEndpoints
{
    public static void MapLoanDisbursementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/loan-disbursements")
            .WithTags("LoanDisbursements")
            .WithGroupName("Loan")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllLoanDisbursements")
            .WithSummary("Get all loan disbursements");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetLoanDisbursementById")
            .WithSummary("Get a loan disbursement by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateLoanDisbursementCommand>>()
            .WithName("CreateLoanDisbursement")
            .WithSummary("Create a draft loan disbursement");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateLoanDisbursement")
            .WithSummary("Update a draft loan disbursement");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteLoanDisbursement")
            .WithSummary("Delete a draft loan disbursement");

        group.MapPost("/{id:guid}/submit", Submit)
            .WithName("SubmitLoanDisbursement")
            .WithSummary("Submit a draft loan disbursement for approval");

        group.MapPost("/{id:guid}/approve", Approve)
            .WithName("ApproveLoanDisbursement")
            .WithSummary("Approve a pending loan disbursement — posts the GL journal and activates the loan account");

        group.MapPost("/{id:guid}/reject", Reject)
            .WithName("RejectLoanDisbursement")
            .WithSummary("Reject a pending loan disbursement back to draft");

        group.MapPost("/{id:guid}/cancel", Cancel)
            .WithName("CancelLoanDisbursement")
            .WithSummary("Cancel a draft or pending-approval loan disbursement directly");

        group.MapPost("/{id:guid}/request-cancellation", RequestCancellation)
            .WithName("RequestLoanDisbursementCancellation")
            .WithSummary("Request cancellation of a posted loan disbursement");

        group.MapPost("/{id:guid}/approve-cancellation", ApproveCancellation)
            .WithName("ApproveLoanDisbursementCancellation")
            .WithSummary("Approve a cancellation request — reverses the GL journal and reverts the loan account to pending disbursement");

        group.MapPost("/{id:guid}/reject-cancellation", RejectCancellation)
            .WithName("RejectLoanDisbursementCancellation")
            .WithSummary("Reject a cancellation request — the document stands as posted");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllLoanDisbursementsQuery, Result<List<LoanDisbursementResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllLoanDisbursementsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetLoanDisbursementQuery, Result<LoanDisbursementResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetLoanDisbursementQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateLoanDisbursementCommand command,
        ICommandHandler<CreateLoanDisbursementCommand, Result<LoanDisbursementResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetLoanDisbursementById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateLoanDisbursementRequest request,
        IValidator<UpdateLoanDisbursementCommand> validator,
        ICommandHandler<UpdateLoanDisbursementCommand, Result<LoanDisbursementResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateLoanDisbursementCommand(
            id, request.BranchId, request.DisbursementDate, request.Amount, request.PaymentMethodId,
            request.ReferenceNo, request.CostCenterId, request.Remarks, request.UpdatedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteLoanDisbursementCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteLoanDisbursementCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(
        Guid id,
        SubmitLoanDisbursementRequest request,
        IValidator<SubmitLoanDisbursementCommand> validator,
        ICommandHandler<SubmitLoanDisbursementCommand, Result<LoanDisbursementResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new SubmitLoanDisbursementCommand(id, request.RequestedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Approve(
        Guid id,
        DecideLoanDisbursementRequest request,
        IValidator<ApproveLoanDisbursementCommand> validator,
        ICommandHandler<ApproveLoanDisbursementCommand, Result<LoanDisbursementResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveLoanDisbursementCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reject(
        Guid id,
        DecideLoanDisbursementRequiredCommentRequest request,
        IValidator<RejectLoanDisbursementCommand> validator,
        ICommandHandler<RejectLoanDisbursementCommand, Result<LoanDisbursementResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectLoanDisbursementCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Cancel(
        Guid id,
        CancelLoanDisbursementRequest request,
        IValidator<CancelLoanDisbursementCommand> validator,
        ICommandHandler<CancelLoanDisbursementCommand, Result<LoanDisbursementResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelLoanDisbursementCommand(id, request.CancelledBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RequestCancellation(
        Guid id,
        RequestLoanDisbursementCancellationRequest request,
        IValidator<RequestLoanDisbursementCancellationCommand> validator,
        ICommandHandler<RequestLoanDisbursementCancellationCommand, Result<LoanDisbursementResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RequestLoanDisbursementCancellationCommand(id, request.RequestedBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> ApproveCancellation(
        Guid id,
        DecideLoanDisbursementRequest request,
        IValidator<ApproveLoanDisbursementCancellationCommand> validator,
        ICommandHandler<ApproveLoanDisbursementCancellationCommand, Result<LoanDisbursementResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveLoanDisbursementCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RejectCancellation(
        Guid id,
        DecideLoanDisbursementRequiredCommentRequest request,
        IValidator<RejectLoanDisbursementCancellationCommand> validator,
        ICommandHandler<RejectLoanDisbursementCancellationCommand, Result<LoanDisbursementResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectLoanDisbursementCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateLoanDisbursementRequest(
    string BranchId,
    DateTimeOffset DisbursementDate,
    decimal Amount,
    Guid PaymentMethodId,
    string? ReferenceNo,
    Guid? CostCenterId,
    string? Remarks,
    string? UpdatedBy);

public sealed record SubmitLoanDisbursementRequest(string RequestedBy);
public sealed record DecideLoanDisbursementRequest(string ApproverUserId, string? Comments);
public sealed record DecideLoanDisbursementRequiredCommentRequest(string ApproverUserId, string Comments);
public sealed record CancelLoanDisbursementRequest(string CancelledBy, string Reason);
public sealed record RequestLoanDisbursementCancellationRequest(string RequestedBy, string Reason);
