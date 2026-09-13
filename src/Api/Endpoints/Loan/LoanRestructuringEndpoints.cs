using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanRestructurings.Approve;
using ZARI.Application.Features.Loan.LoanRestructurings.ApproveCancellation;
using ZARI.Application.Features.Loan.LoanRestructurings.Cancel;
using ZARI.Application.Features.Loan.LoanRestructurings.Create;
using ZARI.Application.Features.Loan.LoanRestructurings.Delete;
using ZARI.Application.Features.Loan.LoanRestructurings.Get;
using ZARI.Application.Features.Loan.LoanRestructurings.GetAll;
using ZARI.Application.Features.Loan.LoanRestructurings.Reject;
using ZARI.Application.Features.Loan.LoanRestructurings.RejectCancellation;
using ZARI.Application.Features.Loan.LoanRestructurings.RequestCancellation;
using ZARI.Application.Features.Loan.LoanRestructurings.Submit;
using ZARI.Application.Features.Loan.LoanRestructurings.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class LoanRestructuringEndpoints
{
    public static void MapLoanRestructuringEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/loan-restructurings")
            .WithTags("LoanRestructurings")
            .WithGroupName("Loan")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllLoanRestructurings")
            .WithSummary("Get all loan restructurings");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetLoanRestructuringById")
            .WithSummary("Get a loan restructuring by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateLoanRestructuringCommand>>()
            .WithName("CreateLoanRestructuring")
            .WithSummary("Create a draft loan restructuring");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateLoanRestructuring")
            .WithSummary("Update a draft loan restructuring");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteLoanRestructuring")
            .WithSummary("Delete a draft loan restructuring");

        group.MapPost("/{id:guid}/submit", Submit)
            .WithName("SubmitLoanRestructuring")
            .WithSummary("Submit a draft loan restructuring for approval");

        group.MapPost("/{id:guid}/approve", Approve)
            .WithName("ApproveLoanRestructuring")
            .WithSummary("Approve a pending loan restructuring — spawns a new loan account under the new terms and supersedes the old schedule");

        group.MapPost("/{id:guid}/reject", Reject)
            .WithName("RejectLoanRestructuring")
            .WithSummary("Reject a pending loan restructuring back to draft");

        group.MapPost("/{id:guid}/cancel", Cancel)
            .WithName("CancelLoanRestructuring")
            .WithSummary("Cancel a draft or pending-approval loan restructuring directly");

        group.MapPost("/{id:guid}/request-cancellation", RequestCancellation)
            .WithName("RequestLoanRestructuringCancellation")
            .WithSummary("Request cancellation of a posted loan restructuring");

        group.MapPost("/{id:guid}/approve-cancellation", ApproveCancellation)
            .WithName("ApproveLoanRestructuringCancellation")
            .WithSummary("Approve a cancellation request — unwinds the new loan account and restores the old one");

        group.MapPost("/{id:guid}/reject-cancellation", RejectCancellation)
            .WithName("RejectLoanRestructuringCancellation")
            .WithSummary("Reject a cancellation request — the restructuring stands as posted");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllLoanRestructuringsQuery, Result<List<LoanRestructuringResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllLoanRestructuringsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetLoanRestructuringQuery, Result<LoanRestructuringResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetLoanRestructuringQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateLoanRestructuringCommand command,
        ICommandHandler<CreateLoanRestructuringCommand, Result<LoanRestructuringResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetLoanRestructuringById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateLoanRestructuringRequest request,
        IValidator<UpdateLoanRestructuringCommand> validator,
        ICommandHandler<UpdateLoanRestructuringCommand, Result<LoanRestructuringResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateLoanRestructuringCommand(
            id, request.BranchId, request.RestructureDate, request.NewAnnualInterestRatePct, request.NewTermMonths, request.NewRepaymentFrequency,
            request.NewGracePeriodDays, request.NewPenaltyRatePct, request.NewFirstDueDate, request.Reason, request.Remarks, request.UpdatedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteLoanRestructuringCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteLoanRestructuringCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(
        Guid id,
        SubmitLoanRestructuringRequest request,
        IValidator<SubmitLoanRestructuringCommand> validator,
        ICommandHandler<SubmitLoanRestructuringCommand, Result<LoanRestructuringResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new SubmitLoanRestructuringCommand(id, request.RequestedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Approve(
        Guid id,
        DecideLoanRestructuringRequest request,
        IValidator<ApproveLoanRestructuringCommand> validator,
        ICommandHandler<ApproveLoanRestructuringCommand, Result<LoanRestructuringResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveLoanRestructuringCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reject(
        Guid id,
        DecideLoanRestructuringRequiredCommentRequest request,
        IValidator<RejectLoanRestructuringCommand> validator,
        ICommandHandler<RejectLoanRestructuringCommand, Result<LoanRestructuringResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectLoanRestructuringCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Cancel(
        Guid id,
        CancelLoanRestructuringRequest request,
        IValidator<CancelLoanRestructuringCommand> validator,
        ICommandHandler<CancelLoanRestructuringCommand, Result<LoanRestructuringResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelLoanRestructuringCommand(id, request.CancelledBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RequestCancellation(
        Guid id,
        RequestLoanRestructuringCancellationRequest request,
        IValidator<RequestLoanRestructuringCancellationCommand> validator,
        ICommandHandler<RequestLoanRestructuringCancellationCommand, Result<LoanRestructuringResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RequestLoanRestructuringCancellationCommand(id, request.RequestedBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> ApproveCancellation(
        Guid id,
        DecideLoanRestructuringRequest request,
        IValidator<ApproveLoanRestructuringCancellationCommand> validator,
        ICommandHandler<ApproveLoanRestructuringCancellationCommand, Result<LoanRestructuringResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveLoanRestructuringCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RejectCancellation(
        Guid id,
        DecideLoanRestructuringRequiredCommentRequest request,
        IValidator<RejectLoanRestructuringCancellationCommand> validator,
        ICommandHandler<RejectLoanRestructuringCancellationCommand, Result<LoanRestructuringResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectLoanRestructuringCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateLoanRestructuringRequest(
    string BranchId,
    DateTimeOffset RestructureDate,
    decimal NewAnnualInterestRatePct,
    int NewTermMonths,
    string NewRepaymentFrequency,
    int NewGracePeriodDays,
    decimal NewPenaltyRatePct,
    DateTimeOffset NewFirstDueDate,
    string Reason,
    string? Remarks,
    string? UpdatedBy);

public sealed record SubmitLoanRestructuringRequest(string RequestedBy);
public sealed record DecideLoanRestructuringRequest(string ApproverUserId, string? Comments);
public sealed record DecideLoanRestructuringRequiredCommentRequest(string ApproverUserId, string Comments);
public sealed record CancelLoanRestructuringRequest(string CancelledBy, string Reason);
public sealed record RequestLoanRestructuringCancellationRequest(string RequestedBy, string Reason);
