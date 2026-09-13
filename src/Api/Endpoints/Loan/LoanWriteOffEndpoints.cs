using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanWriteOffs.Approve;
using ZARI.Application.Features.Loan.LoanWriteOffs.ApproveCancellation;
using ZARI.Application.Features.Loan.LoanWriteOffs.Cancel;
using ZARI.Application.Features.Loan.LoanWriteOffs.Create;
using ZARI.Application.Features.Loan.LoanWriteOffs.Delete;
using ZARI.Application.Features.Loan.LoanWriteOffs.Get;
using ZARI.Application.Features.Loan.LoanWriteOffs.GetAll;
using ZARI.Application.Features.Loan.LoanWriteOffs.Reject;
using ZARI.Application.Features.Loan.LoanWriteOffs.RejectCancellation;
using ZARI.Application.Features.Loan.LoanWriteOffs.RequestCancellation;
using ZARI.Application.Features.Loan.LoanWriteOffs.Submit;
using ZARI.Application.Features.Loan.LoanWriteOffs.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class LoanWriteOffEndpoints
{
    public static void MapLoanWriteOffEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/loan-write-offs")
            .WithTags("LoanWriteOffs")
            .WithGroupName("Loan")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllLoanWriteOffs")
            .WithSummary("Get all loan write-offs");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetLoanWriteOffById")
            .WithSummary("Get a loan write-off by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateLoanWriteOffCommand>>()
            .WithName("CreateLoanWriteOff")
            .WithSummary("Create a draft loan write-off");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateLoanWriteOff")
            .WithSummary("Update a draft loan write-off");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteLoanWriteOff")
            .WithSummary("Delete a draft loan write-off");

        group.MapPost("/{id:guid}/submit", Submit)
            .WithName("SubmitLoanWriteOff")
            .WithSummary("Submit a draft loan write-off for approval");

        group.MapPost("/{id:guid}/approve", Approve)
            .WithName("ApproveLoanWriteOff")
            .WithSummary("Approve a pending loan write-off — requires elevated HQ-only authority; posts the write-off and marks the loan account WRITTEN_OFF");

        group.MapPost("/{id:guid}/reject", Reject)
            .WithName("RejectLoanWriteOff")
            .WithSummary("Reject a pending loan write-off back to draft");

        group.MapPost("/{id:guid}/cancel", Cancel)
            .WithName("CancelLoanWriteOff")
            .WithSummary("Cancel a draft or pending-approval loan write-off directly");

        group.MapPost("/{id:guid}/request-cancellation", RequestCancellation)
            .WithName("RequestLoanWriteOffCancellation")
            .WithSummary("Request cancellation of a posted loan write-off");

        group.MapPost("/{id:guid}/approve-cancellation", ApproveCancellation)
            .WithName("ApproveLoanWriteOffCancellation")
            .WithSummary("Approve a cancellation request — reverses the write-off and restores the loan account to active");

        group.MapPost("/{id:guid}/reject-cancellation", RejectCancellation)
            .WithName("RejectLoanWriteOffCancellation")
            .WithSummary("Reject a cancellation request — the write-off stands as posted");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllLoanWriteOffsQuery, Result<List<LoanWriteOffResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllLoanWriteOffsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetLoanWriteOffQuery, Result<LoanWriteOffResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetLoanWriteOffQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateLoanWriteOffCommand command,
        ICommandHandler<CreateLoanWriteOffCommand, Result<LoanWriteOffResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetLoanWriteOffById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateLoanWriteOffRequest request,
        IValidator<UpdateLoanWriteOffCommand> validator,
        ICommandHandler<UpdateLoanWriteOffCommand, Result<LoanWriteOffResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateLoanWriteOffCommand(
            id, request.BranchId, request.WriteOffDate, request.WriteOffExpenseAccountId, request.Reason, request.Remarks, request.UpdatedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteLoanWriteOffCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteLoanWriteOffCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(
        Guid id,
        SubmitLoanWriteOffRequest request,
        IValidator<SubmitLoanWriteOffCommand> validator,
        ICommandHandler<SubmitLoanWriteOffCommand, Result<LoanWriteOffResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new SubmitLoanWriteOffCommand(id, request.RequestedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Approve(
        Guid id,
        DecideLoanWriteOffRequest request,
        IValidator<ApproveLoanWriteOffCommand> validator,
        ICommandHandler<ApproveLoanWriteOffCommand, Result<LoanWriteOffResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveLoanWriteOffCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reject(
        Guid id,
        DecideLoanWriteOffRequiredCommentRequest request,
        IValidator<RejectLoanWriteOffCommand> validator,
        ICommandHandler<RejectLoanWriteOffCommand, Result<LoanWriteOffResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectLoanWriteOffCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Cancel(
        Guid id,
        CancelLoanWriteOffRequest request,
        IValidator<CancelLoanWriteOffCommand> validator,
        ICommandHandler<CancelLoanWriteOffCommand, Result<LoanWriteOffResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelLoanWriteOffCommand(id, request.CancelledBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RequestCancellation(
        Guid id,
        RequestLoanWriteOffCancellationRequest request,
        IValidator<RequestLoanWriteOffCancellationCommand> validator,
        ICommandHandler<RequestLoanWriteOffCancellationCommand, Result<LoanWriteOffResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RequestLoanWriteOffCancellationCommand(id, request.RequestedBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> ApproveCancellation(
        Guid id,
        DecideLoanWriteOffRequest request,
        IValidator<ApproveLoanWriteOffCancellationCommand> validator,
        ICommandHandler<ApproveLoanWriteOffCancellationCommand, Result<LoanWriteOffResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveLoanWriteOffCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RejectCancellation(
        Guid id,
        DecideLoanWriteOffRequiredCommentRequest request,
        IValidator<RejectLoanWriteOffCancellationCommand> validator,
        ICommandHandler<RejectLoanWriteOffCancellationCommand, Result<LoanWriteOffResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectLoanWriteOffCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateLoanWriteOffRequest(
    string BranchId,
    DateTimeOffset WriteOffDate,
    Guid WriteOffExpenseAccountId,
    string Reason,
    string? Remarks,
    string? UpdatedBy);

public sealed record SubmitLoanWriteOffRequest(string RequestedBy);
public sealed record DecideLoanWriteOffRequest(string ApproverUserId, string? Comments);
public sealed record DecideLoanWriteOffRequiredCommentRequest(string ApproverUserId, string Comments);
public sealed record CancelLoanWriteOffRequest(string CancelledBy, string Reason);
public sealed record RequestLoanWriteOffCancellationRequest(string RequestedBy, string Reason);
